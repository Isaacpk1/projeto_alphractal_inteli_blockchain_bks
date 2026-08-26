import json
import os
import sys
from datetime import datetime, timezone
from pathlib import Path

from dotenv import load_dotenv

from alphractal_etl.alchemy_client import get_block_number, get_fee_history, get_blocks
from alphractal_etl.processor.fees import estimate_fees, unix_to_iso
from alphractal_etl.processor.projection import project_next_base_fee, trend_label

BLOCK_COUNT = 10
REWARD_PERCENTILES = [10, 25, 50, 90]


def main() -> None:
    load_dotenv()
    api_key = os.environ.get("ALCHEMY_API_KEY", "")
    output_dir = Path(os.environ.get("OUTPUT_PATH", "./output"))

    if not api_key:
        print("Error: ALCHEMY_API_KEY not set in .env", file=sys.stderr)
        sys.exit(1)

    print("Fetching latest block number...")
    latest = get_block_number()
    print(f"  Latest block: {latest}")

    print(f"Fetching fee history for last {BLOCK_COUNT} blocks...")
    fee_history = get_fee_history(BLOCK_COUNT, REWARD_PERCENTILES)

    oldest = fee_history["oldest_block"]
    base_fees = fee_history["base_fee_per_gas"]
    gas_ratios = fee_history["gas_used_ratio"]
    rewards = fee_history["reward"]

    print(f"  History window: {oldest} -> {latest}")

    block_numbers = list(range(oldest, latest + 1))
    print(f"Fetching block details for {len(block_numbers)} blocks...")
    blocks_data = get_blocks(block_numbers)

    blocks_out = []
    for i, block in enumerate(blocks_data):
        entry = {
            "block_number": block["number"],
            "hash": block["hash"],
            "timestamp": unix_to_iso(block["timestamp"]),
            "base_fee_per_gas_wei": block["base_fee_per_gas"],
            "gas_used": block["gas_used"],
            "gas_limit": block["gas_limit"],
            "tx_count": block["tx_count"],
        }
        if i < len(rewards) and rewards[i]:
            entry["priority_fee_p10_wei"] = rewards[i][0]
            entry["priority_fee_p25_wei"] = rewards[i][1]
            entry["priority_fee_p50_wei"] = rewards[i][2]
            entry["priority_fee_p90_wei"] = rewards[i][3]
        blocks_out.append(entry)

    latest_reward = rewards[-1] if rewards else [0, 0, 0, 0]
    current_base_fee = base_fees[-1]

    priority_fees = {
        "p10_wei": latest_reward[0],
        "p25_wei": latest_reward[1],
        "p50_wei": latest_reward[2],
        "p90_wei": latest_reward[3],
    }

    by_operation = estimate_fees(current_base_fee, latest_reward)

    latest_block = blocks_out[-1]
    next_base_fee = project_next_base_fee(
        current_base_fee,
        latest_block["gas_used"],
        latest_block["gas_limit"],
    )

    avg_base_fee = sum(base_fees[:BLOCK_COUNT]) // BLOCK_COUNT
    trend = trend_label(current_base_fee, avg_base_fee)

    avg_gas_ratio = (
        sum(gas_ratios) / len(gas_ratios) if gas_ratios else 0.0
    )

    output = {
        "meta": {
            "generated_at": unix_to_iso(
                int(datetime.now(timezone.utc).timestamp())
            ),
            "blocks_analyzed": len(blocks_out),
            "current_block": latest,
            "source": "Alchemy Ethereum Mainnet",
        },
        "fee_estimates": {
            "priority_fee_percentiles": priority_fees,
            "recommended_priority_fee_wei": priority_fees["p50_wei"],
            "by_operation": by_operation,
        },
        "network_status": {
            "base_fee_trend": trend,
            "current_base_fee_wei": current_base_fee,
            "next_base_fee_projection_wei": next_base_fee,
            "avg_gas_used_ratio_10blocks": round(avg_gas_ratio, 4),
        },
        "blocks": blocks_out,
    }

    output_dir.mkdir(parents=True, exist_ok=True)
    out_path = output_dir / "current_state.json"
    out_path.write_text(
        json.dumps(output, indent=2, ensure_ascii=False),
        encoding="utf-8",
    )

    print(f"\nOutput written to: {out_path}")
    print(f"  Current block       : {latest}")
    print(f"  Base fee            : {current_base_fee / 1e9:.2f} gwei")
    print(f"  Next base fee proj  : {next_base_fee / 1e9:.2f} gwei ({trend})")
    print(f"  Rec. priority fee   : {priority_fees['p50_wei'] / 1e9:.2f} gwei")
    print(f"  Avg gas used ratio  : {avg_gas_ratio:.2%}")


if __name__ == "__main__":
    main()