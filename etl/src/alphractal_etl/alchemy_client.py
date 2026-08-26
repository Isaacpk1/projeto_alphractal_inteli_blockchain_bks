import os
import time
from typing import Any

import requests
from dotenv import load_dotenv

load_dotenv()

ALCHEMY_API_KEY = os.getenv("ALCHEMY_API_KEY", "")
ALCHEMY_URL = f"https://eth-mainnet.g.alchemy.com/v2/{ALCHEMY_API_KEY}"
HEADERS = {"Content-Type": "application/json"}


def _rpc(method: str, params: list[Any] | None = None) -> Any:
    payload = {
        "jsonrpc": "2.0",
        "id": int(time.time() * 1000),
        "method": method,
        "params": params or [],
    }
    resp = requests.post(ALCHEMY_URL, json=payload, headers=HEADERS, timeout=30)
    resp.raise_for_status()
    body = resp.json()
    if "error" in body:
        raise RuntimeError(f"RPC error: {body['error']}")
    return body["result"]


def get_block_number() -> int:
    hex_block = _rpc("eth_blockNumber")
    return int(hex_block, 16)


def get_fee_history(block_count: int, reward_percentiles: list[int]) -> dict[str, Any]:
    result = _rpc(
        "eth_feeHistory",
        [hex(block_count), "latest", reward_percentiles],
    )
    return {
        "oldest_block": int(result["oldestBlock"], 16),
        "base_fee_per_gas": [int(x, 16) for x in result["baseFeePerGas"]],
        "gas_used_ratio": result["gasUsedRatio"],
        "reward": [
            [int(v, 16) for v in row]
            for row in result.get("reward", [])
        ],
    }


def get_blocks(block_numbers: list[int]) -> list[dict[str, Any]]:
    batch = []
    for i, num in enumerate(block_numbers):
        batch.append({
            "jsonrpc": "2.0",
            "id": i + 1,
            "method": "eth_getBlockByNumber",
            "params": [hex(num), False],
        })

    resp = requests.post(ALCHEMY_URL, json=batch, headers=HEADERS, timeout=60)
    resp.raise_for_status()
    items = resp.json()

    blocks = []
    for item in items:
        if "error" in item:
            raise RuntimeError(f"RPC error block {item.get('id')}: {item['error']}")
        r = item["result"]
        if not r:
            continue
        blocks.append({
            "number": int(r["number"], 16),
            "hash": r["hash"],
            "timestamp": int(r["timestamp"], 16),
            "base_fee_per_gas": int(r["baseFeePerGas"], 16),
            "gas_used": int(r["gasUsed"], 16),
            "gas_limit": int(r["gasLimit"], 16),
            "tx_count": len(r["transactions"]),
        })

    return sorted(blocks, key=lambda x: x["number"])


def get_max_priority_fee_per_gas() -> int:
    hex_val = _rpc("eth_maxPriorityFeePerGas")
    return int(hex_val, 16)


WEI = 10**9


def wei_to_gwei(wei: int) -> float:
    return wei / WEI


def gwei_to_wei(gwei: float) -> int:
    return int(gwei * WEI)


__all__ = [
    "ALCHEMY_URL",
    "get_block_number",
    "get_fee_history",
    "get_blocks",
    "get_max_priority_fee_per_gas",
    "wei_to_gwei",
    "gwei_to_wei",
]