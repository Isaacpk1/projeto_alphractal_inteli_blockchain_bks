import json
from pathlib import Path
from typing import Any

import pytest

from alphractal_etl.config import EtlConfig
from alphractal_etl.main import process_cycle
from tests.test_contract import block_record


class FakeWriter:
    def __init__(self, fail: bool = False) -> None:
        self.fail = fail
        self.health: list[tuple[str, str, int]] = []

    def insert_batches(self, batches: Any) -> int:
        if self.fail:
            raise RuntimeError("database unavailable")
        return sum(len(rows) for rows in batches.values())

    def write_health(self, status: str, detail: str, last_block: int = 0, lag_ms: int = 0) -> None:
        self.health.append((status, detail, last_block))


def config(root: Path) -> EtlConfig:
    return EtlConfig(root, 1, "localhost", 8123, "alphractal", "etl", "secret", False)


def put_ready(root: Path) -> None:
    ready = root / "ready"
    ready.mkdir(parents=True)
    (ready / "blocks.ndjson").write_text(json.dumps(block_record()) + "\n", encoding="utf-8")


def test_cycle_moves_file_only_after_insert(tmp_path: Path) -> None:
    put_ready(tmp_path)
    writer = FakeWriter()
    assert process_cycle(config(tmp_path), writer) == (1, 0, 1)  # type: ignore[arg-type]
    assert (tmp_path / "processed" / "blocks.ndjson").exists()
    assert writer.health[0][0] == "ok"
    assert writer.health[0][2] == 123


def test_cycle_keeps_file_for_retry_after_insert_failure(tmp_path: Path) -> None:
    put_ready(tmp_path)
    writer = FakeWriter(fail=True)
    with pytest.raises(RuntimeError, match="database unavailable"):
        process_cycle(config(tmp_path), writer)  # type: ignore[arg-type]
    assert (tmp_path / "processing" / "blocks.ndjson").exists()
    assert not (tmp_path / "failed" / "blocks.ndjson").exists()
