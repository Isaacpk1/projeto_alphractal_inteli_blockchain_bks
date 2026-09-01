from __future__ import annotations

from collections.abc import Callable, Mapping
from dataclasses import dataclass, field
from datetime import datetime, timezone
from decimal import Decimal, InvalidOperation
from typing import Any


class ContractError(ValueError):
    pass


def as_uint(value: Any) -> int:
    if isinstance(value, bool):
        raise ContractError("booleano nao e inteiro unsigned")
    try:
        parsed = int(value)
    except (TypeError, ValueError) as exc:
        raise ContractError(f"inteiro invalido: {value!r}") from exc
    if parsed < 0:
        raise ContractError(f"inteiro unsigned negativo: {parsed}")
    return parsed


def as_float(value: Any) -> float:
    try:
        parsed = float(value)
    except (TypeError, ValueError) as exc:
        raise ContractError(f"numero invalido: {value!r}") from exc
    if not 0.0 <= parsed <= 1.0:
        raise ContractError(f"proporcao fora de [0, 1]: {parsed}")
    return parsed


def as_decimal(value: Any) -> Decimal:
    try:
        parsed = Decimal(str(value))
    except (InvalidOperation, ValueError) as exc:
        raise ContractError(f"decimal invalido: {value!r}") from exc
    if not parsed.is_finite() or parsed < 0:
        raise ContractError(f"decimal deve ser finito e nao negativo: {value!r}")
    return parsed


def as_text(value: Any) -> str:
    if not isinstance(value, str) or not value.strip():
        raise ContractError("texto obrigatorio ausente")
    return value.strip()


def as_timestamp(value: Any) -> datetime:
    if isinstance(value, datetime):
        parsed = value
    elif isinstance(value, (int, float)) and not isinstance(value, bool):
        parsed = datetime.fromtimestamp(value, tz=timezone.utc)
    elif isinstance(value, str):
        normalized = value.strip().replace("Z", "+00:00")
        try:
            parsed = datetime.fromisoformat(normalized)
        except ValueError as exc:
            raise ContractError(f"timestamp ISO-8601 invalido: {value!r}") from exc
    else:
        raise ContractError(f"timestamp invalido: {value!r}")
    if parsed.tzinfo is None:
        raise ContractError("timestamp deve incluir timezone")
    return parsed.astimezone(timezone.utc)


Converter = Callable[[Any], Any]


@dataclass(frozen=True)
class TableContract:
    columns: tuple[str, ...]
    converters: dict[str, Converter]
    #: Colunas que podem faltar no arquivo, com o valor assumido quando faltam.
    #:
    #: Existe por causa da compatibilidade entre versoes: quando uma coluna nova
    #: entra, o spool ja tem arquivos escritos pela versao anterior — e o
    #: contrato rejeita o arquivo INTEIRO por um campo ausente, levando junto
    #: todos os blocos que ele carrega. Uma coluna so entra aqui quando o valor
    #: assumido e distinguivel de um dado real (zero para total_fee_wei: o dia
    #: em que a rede cobra zero de taxa nao existe).
    defaults: Mapping[str, Any] = field(default_factory=dict)

    def validate(self, data: Any) -> tuple[Any, ...]:
        if not isinstance(data, dict):
            raise ContractError("data deve ser um objeto JSON")
        missing = [
            column for column in self.columns
            if column not in data and column not in self.defaults
        ]
        if missing:
            raise ContractError(f"campos ausentes: {', '.join(missing)}")
        unknown = sorted(set(data) - set(self.columns))
        if unknown:
            raise ContractError(f"campos desconhecidos: {', '.join(unknown)}")
        return tuple(
            self.converters[column](data[column]) if column in data else self.defaults[column]
            for column in self.columns
        )


TABLE_CONTRACTS: dict[str, TableContract] = {
    "eth_blocks": TableContract(
        columns=(
            "block_number", "block_hash", "block_timestamp", "base_fee_per_gas",
            "next_base_fee", "gas_used", "gas_limit", "tx_count",
            "priority_fee_p10", "priority_fee_p50", "priority_fee_p90",
            "burned_wei", "total_fee_wei", "eth_usd",
        ),
        converters={
            "block_number": as_uint, "block_hash": as_text,
            "block_timestamp": as_timestamp, "base_fee_per_gas": as_uint,
            "next_base_fee": as_uint, "gas_used": as_uint, "gas_limit": as_uint,
            "tx_count": as_uint, "priority_fee_p10": as_uint,
            "priority_fee_p50": as_uint, "priority_fee_p90": as_uint,
            "burned_wei": as_uint, "total_fee_wei": as_uint,
            "eth_usd": as_decimal,
        },
        # Arquivos escritos antes de total_fee_wei existir seguem validos.
        defaults={"total_fee_wei": 0},
    ),
    "mempool_samples": TableContract(
        columns=(
            "sampled_at", "block_number", "pending_tx_count", "base_fee_per_gas",
            "suggested_priority_slow", "suggested_priority_standard",
            "suggested_priority_fast", "eth_usd",
        ),
        converters={
            "sampled_at": as_timestamp, "block_number": as_uint,
            "pending_tx_count": as_uint, "base_fee_per_gas": as_uint,
            "suggested_priority_slow": as_uint,
            "suggested_priority_standard": as_uint,
            "suggested_priority_fast": as_uint, "eth_usd": as_decimal,
        },
    ),
    "fee_estimates": TableContract(
        columns=(
            "sampled_at", "block_number", "operation", "speed", "gas_units",
            "total_fee_wei", "total_fee_gwei", "total_fee_usd",
        ),
        converters={
            "sampled_at": as_timestamp, "block_number": as_uint,
            "operation": as_text, "speed": as_text, "gas_units": as_uint,
            "total_fee_wei": as_uint, "total_fee_gwei": as_decimal,
            "total_fee_usd": as_decimal,
        },
    ),
    "eth_usd_prices": TableContract(
        columns=("observed_at", "source", "price_usd"),
        converters={"observed_at": as_timestamp, "source": as_text, "price_usd": as_decimal},
    ),
    "ingestion_health": TableContract(
        columns=("observed_at", "component", "status", "lag_ms", "last_block", "detail"),
        converters={
            "observed_at": as_timestamp, "component": as_text, "status": as_text,
            "lag_ms": as_uint, "last_block": as_uint, "detail": lambda value: str(value),
        },
    ),
}


def validate_record(record: Any) -> tuple[str, tuple[Any, ...]]:
    if not isinstance(record, dict):
        raise ContractError("registro deve ser um objeto JSON")
    table = record.get("table")
    if table not in TABLE_CONTRACTS:
        raise ContractError(f"tabela nao permitida: {table!r}")
    return table, TABLE_CONTRACTS[table].validate(record.get("data"))
