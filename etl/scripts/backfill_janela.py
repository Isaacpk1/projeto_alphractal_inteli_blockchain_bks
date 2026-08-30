#!/usr/bin/env python3
"""Backfill de varios dias, um dia por vez, com o preco historico de cada dia.

POR QUE NAO UM `backfill` UNICO
O comando `alphractal-etl backfill` aceita um `--eth-usd` por execucao. Usar a
cotacao de hoje para 30 dias atras gravaria preco errado em 216 mil linhas e
corromperia `eth_usd_avg` no rollup — exatamente o tipo de dado que passa
despercebido e invalida a analise financeira depois. Este script quebra a janela
em dias e busca o preco de fechamento de cada um.

O que ele NAO faz: inventar timestamp. Os blocos vem do `eth_getBlockByNumber`
real, em batch JSON-RPC. So o mapeamento data -> faixa de blocos e aproximado
(12 s por slot), e isso nao afeta o dado gravado: as faixas sao contiguas e
cobrem o intervalo inteiro; o timestamp de cada bloco vem da rede.

Uso:
    python scripts/backfill_janela.py --dias 30
    python scripts/backfill_janela.py --dias 7 --batch-size 100 --pausa 0.2
    python scripts/backfill_janela.py --dias 30 --simular   # so mostra o plano

Requer ALCHEMY_API_KEY no ambiente (ou no .env da pasta etl/).
"""
from __future__ import annotations

import argparse
import os
import subprocess
import sys
import time
from datetime import datetime, timedelta, timezone
from decimal import Decimal
from pathlib import Path

import requests
from dotenv import load_dotenv

from alphractal_etl.alchemy_client import AlchemyClient, AlchemyError
from alphractal_etl.config import EtlConfig

# Caminho explicito em vez de load_dotenv() sem argumento: a busca automatica
# parte do arquivo que chamou e sobe diretorios, e o resultado muda conforme o
# script e invocado (modulo, caminho relativo, outro cwd). Falha assim e das
# piores de diagnosticar — o arquivo esta certo, o valor esta la, e a variavel
# simplesmente nao aparece.
RAIZ_ETL = Path(__file__).resolve().parent.parent

SEGUNDOS_POR_BLOCO = 12
BLOCOS_POR_DIA = 24 * 60 * 60 // SEGUNDOS_POR_BLOCO  # 7200
PRECO_URL = "https://api.coinbase.com/v2/prices/ETH-USD/spot"


def preco_do_dia(dia: datetime) -> Decimal:
    """Cotacao ETH/USD daquela data, pela Coinbase (aceita ?date=YYYY-MM-DD)."""
    resposta = requests.get(
        PRECO_URL, params={"date": dia.strftime("%Y-%m-%d")}, timeout=20
    )
    resposta.raise_for_status()
    valor = resposta.json()["data"]["amount"]
    preco = Decimal(str(valor))
    if preco <= 0:
        raise ValueError(f"cotacao invalida para {dia:%Y-%m-%d}: {valor!r}")
    return preco


def abrir_clickhouse():
    """Cliente do ClickHouse para consultar cobertura, ou None se indisponivel.

    Indisponibilidade NAO interrompe o backfill: sem o banco perdemos a
    capacidade de pular dias ja carregados, o que custa tempo, nao dado. Abortar
    a extracao inteira por causa disso seria trocar um problema pequeno por um
    grande.
    """
    try:
        import clickhouse_connect

        config = EtlConfig.from_env()
        cliente = clickhouse_connect.get_client(
            host=config.clickhouse_host,
            port=config.clickhouse_port,
            username=config.clickhouse_user,
            password=config.clickhouse_password,
            database=config.clickhouse_database,
            secure=config.clickhouse_secure,
        )
        cliente.command("SELECT 1")
        return cliente
    except Exception as erro:  # noqa: BLE001
        print(f"aviso: sem ClickHouse para verificar cobertura ({erro}); "
              f"nenhum dia sera pulado", file=sys.stderr)
        return None


def blocos_ja_carregados(cliente, inicio: int, fim: int) -> int:
    """Quantos blocos DISTINTOS da faixa ja estao no banco.

    `uniqExact` e nao `count`: a tabela e ReplacingMergeTree e aceita entrega
    at-least-once, entao o mesmo bloco pode ter varias linhas antes da fusao.
    Contar linhas daria cobertura acima de 100% e pularia dia incompleto.
    """
    resultado = cliente.query(
        "SELECT uniqExact(block_number) FROM eth_blocks "
        "WHERE block_number >= {inicio:UInt64} AND block_number <= {fim:UInt64}",
        parameters={"inicio": inicio, "fim": fim},
    )
    return int(resultado.result_rows[0][0])


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--dias", type=int, default=30, help="quantos dias para tras (padrao: 30)")
    parser.add_argument("--batch-size", type=int, default=20,
                        help="blocos por chamada RPC (padrao: 20). O teto nao e o tamanho da "
                             "resposta, e o limite de unidades por SEGUNDO da Alchemy: "
                             "~16 CU por bloco contra ~330 CU/s no plano gratuito")
    parser.add_argument("--pausa-lote", type=float, default=1.0, dest="pausa_lote",
                        help="segundos entre requisicoes RPC (padrao: 1.0). Com o batch padrao "
                             "isso da ~20 blocos/s, dentro do limite gratuito")
    parser.add_argument("--blocks-per-file", type=int, default=1800,
                        help="blocos por arquivo de spool (padrao: 1800 = 6 h). Menos arquivos, "
                             "menos INSERTs, menos risco de TOO_MANY_PARTS")
    parser.add_argument("--pausa", type=float, default=0.0, help="segundos entre dias, para aliviar rate limit")
    parser.add_argument("--simular", action="store_true", help="mostra o plano e sai, sem chamar a rede")
    parser.add_argument("--forcar", action="store_true",
                        help="refaz dias que ja estao completos no banco")
    args = parser.parse_args()

    if args.dias < 1:
        print("--dias deve ser >= 1", file=sys.stderr)
        return 2

    env = RAIZ_ETL / ".env"
    load_dotenv(env)

    chave = os.getenv("ALCHEMY_API_KEY", "").strip()
    if not chave:
        onde = env if env.is_file() else f"{env} (arquivo nao existe)"
        print(f"ALCHEMY_API_KEY nao configurada. Procurei em: {onde}", file=sys.stderr)
        return 2

    cliente = AlchemyClient(chave)
    try:
        topo = cliente.get_block_number()
    except AlchemyError as erro:
        print(f"nao foi possivel obter o bloco atual: {erro}", file=sys.stderr)
        return 1
    finally:
        cliente.close()

    agora = datetime.now(timezone.utc)
    total_blocos = args.dias * BLOCOS_POR_DIA
    print(f"bloco atual: {topo}")
    print(f"janela: {args.dias} dia(s) ≈ {total_blocos} blocos")
    requisicoes = total_blocos // args.batch_size * 2
    minutos = requisicoes * args.pausa_lote / 60
    print(f"  ~{requisicoes} requisicoes HTTP (lotes de {args.batch_size})")
    print(f"  ~{minutos:.0f} min a {args.pausa_lote}s por requisicao")
    print(f"  ~{max(1, total_blocos // args.blocks_per_file)} arquivos de spool "
          f"(de {args.blocks_per_file} blocos)")
    print()

    falhas: list[str] = []
    pulados = 0

    # Retomada: uma corrida de 30 dias leva horas, e refazer do zero apos uma
    # interrupcao gastaria cota e tempo por dado que ja esta no banco.
    clickhouse = None if args.simular or args.forcar else abrir_clickhouse()

    # Do mais antigo para o mais novo: se interromper no meio, o que ficou
    # gravado e um prefixo continuo do historico, nao buracos espalhados.
    for offset in range(args.dias, 0, -1):
        dia = agora - timedelta(days=offset)
        fim = topo - (offset - 1) * BLOCOS_POR_DIA - 1
        inicio = fim - BLOCOS_POR_DIA + 1
        if inicio < 1:
            continue

        rotulo = f"{dia:%Y-%m-%d}"
        esperados = fim - inicio + 1

        if args.simular:
            print(f"[{rotulo}] blocos {inicio}..{fim}")
            continue

        if clickhouse is not None:
            try:
                carregados = blocos_ja_carregados(clickhouse, inicio, fim)
            except Exception as erro:  # noqa: BLE001
                print(f"[{rotulo}] aviso: consulta de cobertura falhou ({erro})", file=sys.stderr)
                carregados = 0

            cobertura = carregados / esperados if esperados else 0
            # 99,5% e nao 100%: o ultimo bloco de um dia pode estar no arquivo
            # ainda em transito para o ETL quando a consulta roda.
            if cobertura >= 0.995:
                print(f"[{rotulo}] ja carregado ({carregados}/{esperados}); pulado")
                pulados += 1
                continue
            if carregados > 0:
                print(f"[{rotulo}] parcial ({carregados}/{esperados} = {cobertura:.0%}); refazendo o dia")

        try:
            preco = preco_do_dia(dia)
        except Exception as erro:  # noqa: BLE001 — qualquer falha aqui pula o dia
            print(f"[{rotulo}] SEM COTACAO ({erro}); dia pulado — "
                  f"gravar preco errado e pior que faltar o dia", file=sys.stderr)
            falhas.append(rotulo)
            continue

        print(f"[{rotulo}] blocos {inicio}..{fim} · ETH/USD {preco}", flush=True)

        resultado = subprocess.run(
            [sys.executable, "-m", "alphractal_etl", "backfill",
             "--from-block", str(inicio), "--to-block", str(fim),
             "--eth-usd", str(preco), "--batch-size", str(args.batch_size),
             "--blocks-per-file", str(args.blocks_per_file),
             "--pausa-lote", str(args.pausa_lote)],
            check=False,
            cwd=RAIZ_ETL,
        )
        if resultado.returncode != 0:
            print(f"[{rotulo}] backfill falhou (codigo {resultado.returncode})", file=sys.stderr)
            falhas.append(rotulo)

        if args.pausa > 0:
            time.sleep(args.pausa)

    print()
    if pulados:
        print(f"dias pulados por ja estarem completos: {pulados}")
    if falhas:
        print(f"dias com falha ({len(falhas)}): {', '.join(falhas)}", file=sys.stderr)
        print("rode o mesmo comando de novo — os dias completos serao pulados "
              "e so os que faltam sao refeitos", file=sys.stderr)
    print("arquivos gerados em spool/ready; o ETL em container drena sozinho.")
    print("cobertura por dia:")
    print("  docker compose exec clickhouse clickhouse-client --user alphractal "
          "--password alphractal_dev --query \"SELECT toDate(block_timestamp) dia, "
          "uniqExact(block_number) blocos FROM alphractal.eth_blocks GROUP BY dia ORDER BY dia\"")
    return 1 if falhas else 0


if __name__ == "__main__":
    raise SystemExit(main())
