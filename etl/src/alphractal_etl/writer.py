from __future__ import annotations

from collections.abc import Mapping, Sequence
from datetime import datetime, timezone
from typing import Any, Protocol, cast

from alphractal_etl.config import EtlConfig
from alphractal_etl.contract import TABLE_CONTRACTS


class ClickHouseClient(Protocol):
    def command(self, cmd: str, parameters: Mapping[str, Any] | None = None) -> Any: ...
    def insert(
        self,
        table: str,
        data: Sequence[Sequence[Any]],
        column_names: Sequence[str],
        settings: Mapping[str, Any] | None = None,
    ) -> Any: ...
    def close(self) -> None: ...


class ClickHouseWriter:
    def __init__(self, config: EtlConfig, client: ClickHouseClient | None = None) -> None:
        self.config = config
        if client is None:
            import clickhouse_connect

            actual_client = cast(ClickHouseClient, clickhouse_connect.get_client(
                host=config.clickhouse_host,
                port=config.clickhouse_port,
                username=config.clickhouse_user,
                password=config.clickhouse_password,
                database=config.clickhouse_database,
                secure=config.clickhouse_secure,
            ))
        else:
            actual_client = client
        self.client: ClickHouseClient = actual_client

    def ping(self) -> None:
        self.client.command("SELECT 1")

    def insert_batches(self, batches: Mapping[str, list[tuple[Any, ...]]]) -> int:
        total = 0
        for table, rows in batches.items():
            if not rows:
                continue
            contract = TABLE_CONTRACTS[table]
            self.client.insert(
                f"{self.config.clickhouse_database}.{table}",
                rows,
                column_names=contract.columns,
                settings={"async_insert": 1, "wait_for_async_insert": 1},
            )
            total += len(rows)
        self._refresh_affected_rollups(batches)
        return total

    def write_health(self, status: str, detail: str, last_block: int = 0, lag_ms: int = 0) -> None:
        contract = TABLE_CONTRACTS["ingestion_health"]
        row = (
            datetime.now(timezone.utc), "etl", status, max(0, lag_ms),
            max(0, last_block), detail[:1000],
        )
        self.client.insert(
            f"{self.config.clickhouse_database}.ingestion_health",
            [row],
            column_names=contract.columns,
            settings={"async_insert": 1, "wait_for_async_insert": 1},
        )

    def close(self) -> None:
        self.client.close()

    def _refresh_affected_rollups(self, batches: Mapping[str, list[tuple[Any, ...]]]) -> None:
        database = self.config.clickhouse_database
        block_rows = batches.get("eth_blocks", [])
        if block_rows:
            timestamps = [row[2] for row in block_rows]
            params = {"start": min(timestamps), "end": max(timestamps)}
            self.client.command(_BLOCK_ROLLUP_SQL.format(database=database), parameters=params)
        estimate_rows = batches.get("fee_estimates", [])
        if estimate_rows:
            timestamps = [row[0] for row in estimate_rows]
            params = {"start": min(timestamps), "end": max(timestamps)}
            self.client.command(_ESTIMATE_ROLLUP_SQL.format(database=database), parameters=params)


_BLOCK_ROLLUP_SQL = """
INSERT INTO {database}.eth_fees_rollup
SELECT
    granularity,
    bucket,
    now64(3, 'UTC') AS calculated_at,
    count() AS blocks,
    avg(base_fee_per_gas) AS base_fee_avg,
    min(base_fee_per_gas) AS base_fee_min,
    max(base_fee_per_gas) AS base_fee_max,
    quantileExact(0.50)(base_fee_per_gas) AS base_fee_p50,
    quantileExact(0.90)(base_fee_per_gas) AS base_fee_p90,
    quantileExact(0.95)(base_fee_per_gas) AS base_fee_p95,
    avg(priority_fee_p50) AS priority_fee_avg,
    avg(gas_used / greatest(gas_limit, 1)) AS gas_used_ratio_avg,
    sum(tx_count) AS tx_count,
    sum(burned_wei) AS burned_wei,
    sum(total_fee_wei) AS total_fee_wei,
    avg(eth_usd) AS eth_usd_avg
FROM
(
    SELECT 'hour' AS granularity, toStartOfHour(block_timestamp) AS bucket, *
    FROM {database}.eth_blocks FINAL
    WHERE block_timestamp >= toStartOfHour({{start:DateTime64(3, 'UTC')}})
      AND block_timestamp < addHours(toStartOfHour({{end:DateTime64(3, 'UTC')}}), 1)
    UNION ALL
    SELECT 'day' AS granularity, toStartOfDay(block_timestamp) AS bucket, *
    FROM {database}.eth_blocks FINAL
    WHERE block_timestamp >= toStartOfDay({{start:DateTime64(3, 'UTC')}})
      AND block_timestamp < addDays(toStartOfDay({{end:DateTime64(3, 'UTC')}}), 1)
)
GROUP BY granularity, bucket
"""

_ESTIMATE_ROLLUP_SQL = """
INSERT INTO {database}.fee_estimates_1d
SELECT
    toDate(sampled_at) AS bucket,
    operation,
    speed,
    now64(3, 'UTC') AS calculated_at,
    count() AS samples,
    avg(total_fee_usd) AS usd_avg,
    min(total_fee_usd) AS usd_min,
    max(total_fee_usd) AS usd_max,
    quantileExact(0.50)(total_fee_usd) AS usd_p50,
    quantileExact(0.90)(total_fee_usd) AS usd_p90
FROM {database}.fee_estimates FINAL
WHERE sampled_at >= toStartOfDay({{start:DateTime64(3, 'UTC')}})
  AND sampled_at < addDays(toStartOfDay({{end:DateTime64(3, 'UTC')}}), 1)
GROUP BY bucket, operation, speed
"""
