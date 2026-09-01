from __future__ import annotations

import json
import os
from dataclasses import dataclass
from datetime import datetime, timezone
from decimal import Decimal, InvalidOperation
from pathlib import Path

from alphractal_etl.alchemy_client import AlchemyClient, AlchemyError

REWARD_PERCENTILES = [10, 50, 90]


@dataclass(frozen=True)
class BackfillConfig:
    from_block: int
    to_block: int
    eth_usd: Decimal
    batch_size: int
    spool_path: Path
    alchemy_api_key: str
    # Tamanho do lote RPC e tamanho do arquivo de spool sao pressoes OPOSTAS e
    # por isso viraram dois numeros. O batch de `eth_getBlockByNumber` devolve a
    # lista de hashes de transacao de cada bloco: mil blocos passam de 15 MB numa
    # unica resposta e a requisicao arrasta ou estoura o timeout. Ja o arquivo de
    # spool quer ser grande, porque cada arquivo vira um INSERT no ClickHouse, e
    # centenas de INSERTs pequenos levam a `TOO_MANY_PARTS` — o erro que o
    # 09 secao 3 descreve como o numero um de quem chega vindo do Postgres.
    # Zero mantem o comportamento antigo: um arquivo por lote.
    blocks_per_file: int = 0
    # Segundos entre requisicoes. O limite da Alchemy e por SEGUNDO, entao o que
    # protege e o ritmo, nao o total. Um lote de N blocos custa ~16xN unidades;
    # no plano gratuito (~330 CU/s) isso da ~20 blocos por segundo.
    intervalo_minimo: float = 0.0
    # Blocos por chamada de `eth_getBlockReceipts`, que preenche total_fee_wei.
    #
    # Lote PROPRIO e muito menor que o de blocos porque a resposta e de outra
    # ordem de grandeza: o recibo carrega os logs de cada transacao, entao um
    # bloco cheio passa de 1 MB e um lote de 100 estouraria o timeout.
    #
    # Zero desliga a coleta e grava total_fee_wei = 0 — util para refazer so a
    # serie de base fee sem pagar o custo dos recibos. Com zero, o painel
    # continua caindo na estimativa antiga naquele periodo.
    recibos_por_lote: int = 8

    @classmethod
    def from_values(
        cls,
        from_block: int,
        to_block: int,
        eth_usd: str,
        batch_size: int,
        blocks_per_file: int = 0,
        intervalo_minimo: float = 0.0,
        recibos_por_lote: int = 8,
    ) -> "BackfillConfig":
        if from_block < 0 or to_block < from_block:
            raise ValueError("intervalo de blocos invalido")
        if not 1 <= batch_size <= 1024:
            raise ValueError("batch-size deve estar entre 1 e 1024")
        if blocks_per_file < 0:
            raise ValueError("blocks-per-file nao pode ser negativo")
        if 0 < blocks_per_file < batch_size:
            raise ValueError("blocks-per-file deve ser >= batch-size")
        if intervalo_minimo < 0:
            raise ValueError("pausa-lote nao pode ser negativa")
        if not 0 <= recibos_por_lote <= 64:
            raise ValueError("recibos-por-lote deve estar entre 0 e 64")
        try:
            price = Decimal(eth_usd)
        except InvalidOperation as exc:
            raise ValueError("eth-usd invalido") from exc
        if not price.is_finite() or price <= 0:
            raise ValueError("eth-usd deve ser positivo")
        api_key = os.getenv("ALCHEMY_API_KEY", "").strip()
        if not api_key:
            raise ValueError("ALCHEMY_API_KEY nao configurada")
        return cls(
            from_block=from_block, to_block=to_block, eth_usd=price, batch_size=batch_size,
            spool_path=Path(os.getenv("SPOOL_PATH", "../spool")).resolve(),
            alchemy_api_key=api_key,
            blocks_per_file=blocks_per_file,
            intervalo_minimo=intervalo_minimo,
            recibos_por_lote=recibos_por_lote,
        )


def run_backfill(config: BackfillConfig, client: AlchemyClient | None = None) -> list[Path]:
    owns_client = client is None
    if client is None:
        client = AlchemyClient(config.alchemy_api_key, intervalo_minimo=config.intervalo_minimo)
    ready = config.spool_path / "ready"
    ready.mkdir(parents=True, exist_ok=True)
    generated: list[Path] = []

    # Zero = comportamento antigo (um arquivo por lote de RPC).
    per_file = config.blocks_per_file or config.batch_size

    pendentes: list[str] = []
    primeiro_bloco: int | None = None
    ultimo_bloco: int | None = None

    def descarregar() -> None:
        """Fecha o arquivo corrente, se houver linhas acumuladas."""
        nonlocal pendentes, primeiro_bloco, ultimo_bloco
        if not pendentes:
            return
        destination = ready / f"backfill-blocks-{primeiro_bloco}-{ultimo_bloco}.ndjson"
        if destination.exists():
            raise FileExistsError(f"arquivo de backfill ja existe: {destination}")
        # Escreve em .tmp e move: o ETL varre apenas *.ndjson em ready/, entao
        # nunca enxerga um arquivo pela metade. O move e atomico no mesmo volume.
        temporary = destination.with_suffix(".tmp")
        temporary.write_text("\n".join(pendentes) + "\n", encoding="utf-8")
        temporary.replace(destination)
        generated.append(destination)
        pendentes = []
        primeiro_bloco = None
        ultimo_bloco = None

    try:
        start = config.from_block
        while start <= config.to_block:
            end = min(start + config.batch_size - 1, config.to_block)
            block_numbers = list(range(start, end + 1))
            history = client.get_fee_history(len(block_numbers), end, REWARD_PERCENTILES)
            if history["oldest_block"] != start:
                raise AlchemyError(f"janela inesperada: esperado {start}, recebido {history['oldest_block']}")
            blocks = client.get_blocks(block_numbers)
            totais = _coletar_totais(client, block_numbers, config.recibos_por_lote)
            rewards = history["reward"]
            base_fees = history["base_fee_per_gas"]
            if len(rewards) != len(blocks):
                raise AlchemyError("reward ausente no backfill")

            for index, block in enumerate(blocks):
                reward = rewards[index]
                if len(reward) != len(REWARD_PERCENTILES):
                    raise AlchemyError(f"reward incompleto no bloco {block['number']}")
                timestamp = datetime.fromtimestamp(block["timestamp"], tz=timezone.utc)
                data = {
                    "block_number": block["number"], "block_hash": block["hash"],
                    "block_timestamp": timestamp.isoformat().replace("+00:00", "Z"),
                    "base_fee_per_gas": base_fees[index], "next_base_fee": base_fees[index + 1],
                    "gas_used": block["gas_used"], "gas_limit": block["gas_limit"],
                    "tx_count": block["tx_count"], "priority_fee_p10": reward[0],
                    "priority_fee_p50": reward[1], "priority_fee_p90": reward[2],
                    "burned_wei": block["base_fee_per_gas"] * block["gas_used"],
                    "total_fee_wei": totais.get(block["number"], 0),
                    "eth_usd": str(config.eth_usd),
                }
                if primeiro_bloco is None:
                    primeiro_bloco = block["number"]
                ultimo_bloco = block["number"]
                pendentes.append(json.dumps({"table": "eth_blocks", "data": data}, separators=(",", ":")))

            if primeiro_bloco is not None and ultimo_bloco is not None:
                if ultimo_bloco - primeiro_bloco + 1 >= per_file:
                    descarregar()

            start = end + 1

        descarregar()
    finally:
        if owns_client:
            client.close()
    return generated


def _coletar_totais(
    client: AlchemyClient,
    block_numbers: list[int],
    recibos_por_lote: int,
) -> dict[int, int]:
    """Soma a taxa efetivamente paga em cada bloco, em sub-lotes.

    Sub-lote proprio porque a resposta de recibos e ordens de grandeza maior que
    a de cabecalhos: pedir os recibos dos mesmos 100 blocos do lote de
    `get_blocks` traria dezenas de MB numa unica resposta.
    """
    if recibos_por_lote <= 0:
        return {}
    totais: dict[int, int] = {}
    for inicio in range(0, len(block_numbers), recibos_por_lote):
        fatia = block_numbers[inicio:inicio + recibos_por_lote]
        for numero, (total_fee_wei, _tx_count) in client.get_block_fee_totals(fatia).items():
            totais[numero] = total_fee_wei
    return totais
