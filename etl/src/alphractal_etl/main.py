from __future__ import annotations

import argparse
import logging
import sys
import time
from pathlib import Path

from dotenv import load_dotenv

from alphractal_etl.backfill.runner import BackfillConfig, run_backfill
from alphractal_etl.config import EtlConfig
from alphractal_etl.spool import Spool, SpoolError
from alphractal_etl.writer import ClickHouseWriter

LOGGER = logging.getLogger("alphractal_etl")


def process_cycle(config: EtlConfig, writer: ClickHouseWriter) -> tuple[int, int, int]:
    spool = Spool(config.spool_path)
    files = spool.claim_all()
    processed_files = 0
    failed_files = 0
    inserted_rows = 0
    last_block = 0

    for claimed in files:
        try:
            batches = spool.read(claimed)
        except SpoolError as exc:
            failed_files += 1
            reason = f"{type(exc).__name__}: {exc}"
            spool.reject(claimed, reason)
            LOGGER.error("contrato invalido enviado para failed: %s", claimed.original_name)
            continue

        block_rows = batches.get("eth_blocks", [])
        if block_rows:
            last_block = max(last_block, max(int(row[0]) for row in block_rows))
        # Falha de infraestrutura nao e defeito do arquivo: ele permanece em
        # processing para retry no proximo ciclo.
        inserted_rows += writer.insert_batches(batches)
        spool.complete(claimed)
        processed_files += 1
        LOGGER.info("arquivo processado: %s", claimed.original_name)

    status = "degraded" if failed_files else "ok"
    detail = f"files={processed_files}; failed={failed_files}; rows={inserted_rows}" if files else "idle"
    writer.write_health(status, detail, last_block=last_block)
    return processed_files, failed_files, inserted_rows


def run(config: EtlConfig, watch: bool) -> int:
    writer = ClickHouseWriter(config)
    try:
        writer.ping()
        while True:
            try:
                processed, failed, rows = process_cycle(config, writer)
                LOGGER.info("ciclo concluido: files=%d failed=%d rows=%d", processed, failed, rows)
                if not watch:
                    return 1 if failed else 0
            except Exception as exc:
                LOGGER.exception("falha de infraestrutura; arquivo sera reprocessado")
                try:
                    writer.write_health("degraded", f"{type(exc).__name__}: {exc}")
                except Exception:
                    LOGGER.exception("nao foi possivel gravar heartbeat degraded")
                if not watch:
                    return 1
            time.sleep(config.poll_seconds)
    except KeyboardInterrupt:
        LOGGER.info("encerrado pelo usuario")
        return 0
    finally:
        writer.close()


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="ETL Alphractal Fees")
    parser.add_argument("--env-file", type=Path, default=Path(".env"))
    parser.add_argument("--log-level", default="INFO")
    subparsers = parser.add_subparsers(dest="command", required=True)

    run_parser = subparsers.add_parser("run", help="drena o spool para o ClickHouse")
    run_parser.add_argument("--watch", action="store_true", help="continua monitorando o spool")

    backfill_parser = subparsers.add_parser("backfill", help="gera arquivos de spool historicos")
    backfill_parser.add_argument("--from-block", type=int, required=True)
    backfill_parser.add_argument("--to-block", type=int, required=True)
    backfill_parser.add_argument("--eth-usd", required=True, help="preco ETH/USD da janela")
    backfill_parser.add_argument(
        "--batch-size", type=int, default=100,
        help="blocos por chamada RPC (1-1024). Lotes grandes arrastam: cada bloco "
             "traz a lista de hashes de transacao")
    backfill_parser.add_argument(
        "--blocks-per-file", type=int, default=0,
        help="blocos por arquivo de spool (>= batch-size). Arquivos maiores = menos "
             "INSERTs no ClickHouse. 0 usa o batch-size")
    backfill_parser.add_argument(
        "--pausa-lote", type=float, default=0.0, dest="pausa_lote",
        help="segundos entre requisicoes RPC. O limite da Alchemy e por SEGUNDO: "
             "sem ritmo, uma rajada estoura mesmo com poucos lotes")
    return parser


def main() -> None:
    parser = build_parser()
    args = parser.parse_args()
    load_dotenv(args.env_file)
    logging.basicConfig(
        level=getattr(logging, args.log_level.upper(), logging.INFO),
        format="%(asctime)s %(levelname)s %(name)s %(message)s",
    )
    try:
        if args.command == "run":
            raise SystemExit(run(EtlConfig.from_env(), watch=args.watch))
        config = BackfillConfig.from_values(
            from_block=args.from_block,
            to_block=args.to_block,
            eth_usd=args.eth_usd,
            batch_size=args.batch_size,
            blocks_per_file=args.blocks_per_file,
            intervalo_minimo=args.pausa_lote,
        )
        generated = run_backfill(config)
        LOGGER.info("backfill concluido: %d arquivos", len(generated))
    except ValueError as exc:
        LOGGER.error("configuracao invalida: %s", exc)
        sys.exit(2)


if __name__ == "__main__":
    main()
