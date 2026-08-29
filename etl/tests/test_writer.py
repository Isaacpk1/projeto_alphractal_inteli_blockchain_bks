from pathlib import Path
from typing import Any

from alphractal_etl.config import EtlConfig
from alphractal_etl.contract import validate_record
from alphractal_etl.writer import ClickHouseWriter
from tests.test_contract import block_record


class FakeClient:
    def __init__(self) -> None:
        self.inserts: list[tuple[str, Any, Any, Any]] = []
        self.commands: list[tuple[str, Any]] = []
        self.closed = False

    def command(self, cmd: str, parameters: Any = None) -> None:
        self.commands.append((cmd, parameters))

    def insert(self, table: str, data: Any, column_names: Any, settings: Any = None) -> None:
        self.inserts.append((table, data, column_names, settings))

    def close(self) -> None:
        self.closed = True


def config(tmp_path: Path) -> EtlConfig:
    return EtlConfig(tmp_path, 1, "localhost", 8123, "alphractal", "etl", "secret", False)


def test_insert_waits_for_durable_async_flush_and_refreshes_rollup(tmp_path: Path) -> None:
    client = FakeClient()
    writer = ClickHouseWriter(config(tmp_path), client=client)
    _, row = validate_record(block_record())

    assert writer.insert_batches({"eth_blocks": [row]}) == 1
    assert client.inserts[0][0] == "alphractal.eth_blocks"
    assert client.inserts[0][3] == {"async_insert": 1, "wait_for_async_insert": 1}
    assert "eth_fees_rollup" in client.commands[0][0]


def test_health_uses_same_durable_insert_policy(tmp_path: Path) -> None:
    client = FakeClient()
    writer = ClickHouseWriter(config(tmp_path), client=client)
    writer.write_health("ok", "idle")
    assert client.inserts[0][0] == "alphractal.ingestion_health"
    assert client.inserts[0][3]["wait_for_async_insert"] == 1
