from __future__ import annotations

import os
import re
from dataclasses import dataclass
from pathlib import Path


def _required(name: str) -> str:
    value = os.getenv(name, "").strip()
    if not value:
        raise ValueError(f"Variavel obrigatoria ausente: {name}")
    return value


def _boolean(name: str, default: bool = False) -> bool:
    value = os.getenv(name)
    if value is None:
        return default
    normalized = value.strip().lower()
    if normalized in {"1", "true", "yes", "on"}:
        return True
    if normalized in {"0", "false", "no", "off"}:
        return False
    raise ValueError(f"{name} deve ser true ou false")


def _identifier(name: str, default: str) -> str:
    value = os.getenv(name, default).strip()
    if not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", value):
        raise ValueError(f"{name} deve ser um identificador SQL simples")
    return value


@dataclass(frozen=True)
class EtlConfig:
    spool_path: Path
    poll_seconds: float
    clickhouse_host: str
    clickhouse_port: int
    clickhouse_database: str
    clickhouse_user: str
    clickhouse_password: str
    clickhouse_secure: bool

    @classmethod
    def from_env(cls) -> "EtlConfig":
        poll_seconds = float(os.getenv("ETL_POLL_SECONDS", "10"))
        if poll_seconds <= 0:
            raise ValueError("ETL_POLL_SECONDS deve ser maior que zero")
        port = int(os.getenv("CLICKHOUSE_PORT", "8123"))
        if not 1 <= port <= 65535:
            raise ValueError("CLICKHOUSE_PORT invalida")
        return cls(
            spool_path=Path(os.getenv("SPOOL_PATH", "../spool")).resolve(),
            poll_seconds=poll_seconds,
            clickhouse_host=os.getenv("CLICKHOUSE_HOST", "localhost").strip(),
            clickhouse_port=port,
            clickhouse_database=_identifier("CLICKHOUSE_DATABASE", "alphractal"),
            clickhouse_user=_identifier("CLICKHOUSE_USER", "alphractal_etl"),
            clickhouse_password=_required("CLICKHOUSE_PASSWORD"),
            clickhouse_secure=_boolean("CLICKHOUSE_SECURE"),
        )
