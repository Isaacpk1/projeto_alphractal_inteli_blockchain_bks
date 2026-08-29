from __future__ import annotations

import json
import os
from dataclasses import dataclass
from datetime import datetime, timezone
from decimal import Decimal, InvalidOperation
from pathlib import Path

from alphractal_etl.alchemy_client import AlchemyClient, AlchemyError

REWARD_PERCENTILES = [10, 50, 90]


@dataclass(frozen=True)
class BackfillConfig:
    from_block: int
    to_block: int
    eth_usd: Decimal
    batch_size: int
    spool_path: Path
    alchemy_api_key: str

    @classmethod
    def from_values(cls, from_block: int, to_block: int, eth_usd: str, batch_size: int) -> "BackfillConfig":
        if from_block < 0 or to_block < from_block:
            raise ValueError("intervalo de blocos invalido")
        if not 1 <= batch_size <= 1024:
            raise ValueError("batch-size deve estar entre 1 e 1024")
        try:
            price = Decimal(eth_usd)
        except InvalidOperation as exc:
            raise ValueError("eth-usd invalido") from exc
        if not price.is_finite() or price <= 0:
            raise ValueError("eth-usd deve ser positivo")
        api_key = os.getenv("ALCHEMY_API_KEY", "").strip()
        if not api_key:
            raise ValueError("ALCHEMY_API_KEY nao configurada")
        return cls(
            from_block=from_block, to_block=to_block, eth_usd=price, batch_size=batch_size,
            spool_path=Path(os.getenv("SPOOL_PATH", "../spool")).resolve(),
            alchemy_api_key=api_key,
        )


def run_backfill(config: BackfillConfig, client: AlchemyClient | None = None) -> list[Path]:
    owns_client = client is None
    if client is None:
        client = AlchemyClient(config.alchemy_api_key)
    ready = config.spool_path / "ready"
    ready.mkdir(parents=True, exist_ok=True)
    generated: list[Path] = []
    try:
        start = config.from_block
        while start <= config.to_block:
            end = min(start + config.batch_size - 1, config.to_block)
            block_numbers = list(range(start, end + 1))
            history = client.get_fee_history(len(block_numbers), end, REWARD_PERCENTILES)
            if history["oldest_block"] != start:
                raise AlchemyError(f"janela inesperada: esperado {start}, recebido {history['oldest_block']}")
            blocks = client.get_blocks(block_numbers)
            rewards = history["reward"]
            base_fees = history["base_fee_per_gas"]
            if len(rewards) != len(blocks):
                raise AlchemyError("reward ausente no backfill")
            destination = ready / f"backfill-blocks-{start}-{end}.ndjson"
            if destination.exists():
                raise FileExistsError(f"arquivo de backfill ja existe: {destination}")
            lines = []
            for index, block in enumerate(blocks):
                reward = rewards[index]
                if len(reward) != len(REWARD_PERCENTILES):
                    raise AlchemyError(f"reward incompleto no bloco {block['number']}")
                timestamp = datetime.fromtimestamp(block["timestamp"], tz=timezone.utc)
                data = {
                    "block_number": block["number"], "block_hash": block["hash"],
                    "block_timestamp": timestamp.isoformat().replace("+00:00", "Z"),
                    "base_fee_per_gas": base_fees[index], "next_base_fee": base_fees[index + 1],
                    "gas_used": block["gas_used"], "gas_limit": block["gas_limit"],
                    "tx_count": block["tx_count"], "priority_fee_p10": reward[0],
                    "priority_fee_p50": reward[1], "priority_fee_p90": reward[2],
                    "burned_wei": block["base_fee_per_gas"] * block["gas_used"],
                    "eth_usd": str(config.eth_usd),
                }
                lines.append(json.dumps({"table": "eth_blocks", "data": data}, separators=(",", ":")))
            temporary = destination.with_suffix(".tmp")
            temporary.write_text("\n".join(lines) + "\n", encoding="utf-8")
            temporary.replace(destination)
            generated.append(destination)
            start = end + 1
    finally:
        if owns_client:
            client.close()
    return generated
