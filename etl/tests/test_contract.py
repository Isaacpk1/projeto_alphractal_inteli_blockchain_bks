from datetime import timezone
from decimal import Decimal

import pytest

from alphractal_etl.contract import ContractError, validate_record


def block_record() -> dict[str, object]:
    return {
        "table": "eth_blocks",
        "data": {
            "block_number": 123,
            "block_hash": "0xabc",
            "block_timestamp": "2026-08-29T12:30:00Z",
            "base_fee_per_gas": 100,
            "next_base_fee": 101,
            "gas_used": 15_000_000,
            "gas_limit": 30_000_000,
            "tx_count": 10,
            "priority_fee_p10": 1,
            "priority_fee_p50": 2,
            "priority_fee_p90": 3,
            "burned_wei": 1_500_000_000,
            "eth_usd": "3200.123456",
        },
    }


def test_validates_block_without_losing_precision() -> None:
    table, row = validate_record(block_record())
    assert table == "eth_blocks"
    assert row[0] == 123
    assert row[2].tzinfo == timezone.utc
    assert row[-1] == Decimal("3200.123456")


def test_rejects_missing_and_unknown_fields() -> None:
    record = block_record()
    del record["data"]["gas_used"]  # type: ignore[index]
    with pytest.raises(ContractError, match="campos ausentes"):
        validate_record(record)

    record = block_record()
    record["data"]["extra"] = 1  # type: ignore[index]
    with pytest.raises(ContractError, match="campos desconhecidos"):
        validate_record(record)


def test_rejects_timestamp_without_timezone() -> None:
    record = block_record()
    record["data"]["block_timestamp"] = "2026-08-29T12:30:00"  # type: ignore[index]
    with pytest.raises(ContractError, match="timezone"):
        validate_record(record)


def test_rejects_negative_unsigned_value() -> None:
    record = block_record()
    record["data"]["base_fee_per_gas"] = -1  # type: ignore[index]
    with pytest.raises(ContractError, match="negativo"):
        validate_record(record)
