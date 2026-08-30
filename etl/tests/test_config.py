from pathlib import Path

import pytest

from alphractal_etl.config import EtlConfig


def test_reads_valid_configuration(monkeypatch: pytest.MonkeyPatch, tmp_path: Path) -> None:
    monkeypatch.setenv("SPOOL_PATH", str(tmp_path))
    monkeypatch.setenv("CLICKHOUSE_PASSWORD", "secret")
    config = EtlConfig.from_env()
    assert config.spool_path == tmp_path.resolve()
    assert config.clickhouse_database == "alphractal"


def test_rejects_database_identifier_injection(monkeypatch: pytest.MonkeyPatch) -> None:
    monkeypatch.setenv("CLICKHOUSE_PASSWORD", "secret")
    monkeypatch.setenv("CLICKHOUSE_DATABASE", "alphractal; DROP DATABASE alphractal")
    with pytest.raises(ValueError, match="identificador SQL"):
        EtlConfig.from_env()
