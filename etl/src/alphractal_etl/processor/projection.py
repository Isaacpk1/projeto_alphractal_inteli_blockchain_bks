def project_next_base_fee(current_base_fee_wei: int, gas_used: int, gas_limit: int) -> int:
    half = gas_limit // 2
    if gas_used > half:
        return current_base_fee_wei * 1125 // 1000
    elif gas_used < half:
        return current_base_fee_wei * 875 // 1000
    return current_base_fee_wei


def trend_label(current_base_fee_wei: int, avg_base_fee_wei: int) -> str:
    if avg_base_fee_wei == 0:
        return "stable"
    ratio = current_base_fee_wei / avg_base_fee_wei
    if ratio > 1.15:
        return "rising"
    if ratio < 0.85:
        return "falling"
    return "stable"


__all__ = ["project_next_base_fee", "trend_label"]