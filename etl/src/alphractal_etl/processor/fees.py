from datetime import datetime, timezone


def unix_to_iso(ts: int) -> str:
    return datetime.fromtimestamp(ts, tz=timezone.utc).isoformat().replace("+00:00", "Z")


GAS_LIMITS: dict[str, int] = {
    "transfer_eth": 21_000,
    "erc20_transfer": 65_000,
    "approve": 46_000,
    "dex_swap": 150_000,
    "nft_mint": 85_000,
}

SPEEDS: dict[str, int] = {
    "slow": 1,
    "standard": 2,
    "fast": 3,
}


def estimate_fees(
    base_fee_wei: int,
    priority_fees: list[int],
) -> dict[str, dict[str, int]]:
    result: dict[str, dict[str, int]] = {}
    for op_name, gas_limit in GAS_LIMITS.items():
        estimates: dict[str, int] = {}
        for speed, pct_idx in SPEEDS.items():
            fee_wei = (base_fee_wei + priority_fees[pct_idx]) * gas_limit
            estimates[f"{speed}_wei"] = fee_wei
        result[op_name] = estimates
    return result


__all__ = ["GAS_LIMITS", "SPEEDS", "estimate_fees", "unix_to_iso"]