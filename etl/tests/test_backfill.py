import json
from decimal import Decimal
from pathlib import Path

from alphractal_etl.backfill.runner import BackfillConfig, run_backfill


class FakeAlchemy:
    def get_fee_history(self, block_count: int, newest_block: int, reward_percentiles: list[int]):
        assert block_count == 2
        assert newest_block == 11
        assert reward_percentiles == [10, 50, 90]
        return {
            "oldest_block": 10,
            "base_fee_per_gas": [100, 105, 110],
            "gas_used_ratio": [0.5, 0.6],
            "reward": [[1, 2, 3], [4, 5, 6]],
        }

    def get_blocks(self, block_numbers: list[int]):
        return [
            {
                "number": number,
                "hash": f"0x{number}",
                "timestamp": 1_700_000_000 + number,
                "base_fee_per_gas": 100 if number == 10 else 105,
                "gas_used": 10,
                "gas_limit": 20,
                "tx_count": 1,
            }
            for number in block_numbers
        ]


def test_backfill_uses_fee_history_next_base_fee_without_reprojecting(tmp_path: Path) -> None:
    config = BackfillConfig(10, 11, Decimal("3200"), 100, tmp_path, "unused")
    generated = run_backfill(config, client=FakeAlchemy())  # type: ignore[arg-type]
    records = [json.loads(line) for line in generated[0].read_text().splitlines()]
    assert records[0]["data"]["base_fee_per_gas"] == 100
    assert records[0]["data"]["next_base_fee"] == 105
    assert records[1]["data"]["next_base_fee"] == 110
    assert records[1]["data"]["burned_wei"] == 1050
