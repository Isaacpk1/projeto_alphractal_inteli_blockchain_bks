import json
from pathlib import Path

import pytest

from alphractal_etl.spool import Spool, SpoolError
from tests.test_contract import block_record


def write_ndjson(path: Path, records: list[object]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text("\n".join(json.dumps(record) for record in records) + "\n", encoding="utf-8")


def test_claim_read_and_complete(tmp_path: Path) -> None:
    spool = Spool(tmp_path)
    source = tmp_path / "ready" / "blocks.ndjson"
    write_ndjson(source, [block_record()])

    claimed = spool.claim_all()
    assert not source.exists()
    batches = spool.read(claimed[0])
    assert len(batches["eth_blocks"]) == 1

    destination = spool.complete(claimed[0])
    assert destination.parent.name == "processed"
    assert destination.exists()


def test_invalid_file_is_rejected_with_reason(tmp_path: Path) -> None:
    spool = Spool(tmp_path)
    source = tmp_path / "ready" / "invalid.ndjson"
    write_ndjson(source, [{"table": "unknown", "data": {}}])
    claimed = spool.claim_all()[0]

    with pytest.raises(SpoolError) as error:
        spool.read(claimed)
    destination = spool.reject(claimed, str(error.value))

    assert destination.parent.name == "failed"
    assert destination.with_suffix(".ndjson.error.json").exists()


def test_recovers_files_left_in_processing(tmp_path: Path) -> None:
    spool = Spool(tmp_path)
    processing = tmp_path / "processing" / "recovered.ndjson"
    write_ndjson(processing, [block_record()])
    assert spool.claim_all()[0].path == processing
