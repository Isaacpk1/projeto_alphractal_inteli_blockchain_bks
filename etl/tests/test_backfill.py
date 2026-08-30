import json
from decimal import Decimal
from pathlib import Path

from alphractal_etl.backfill.runner import BackfillConfig, run_backfill


class FakeAlchemy:
    def get_fee_history(self, block_count: int, newest_block: int, reward_percentiles: list[int]):
        assert block_count == 2
        assert newest_block == 11
        assert reward_percentiles == [10, 50, 90]
        return {
            "oldest_block": 10,
            "base_fee_per_gas": [100, 105, 110],
            "gas_used_ratio": [0.5, 0.6],
            "reward": [[1, 2, 3], [4, 5, 6]],
        }

    def get_blocks(self, block_numbers: list[int]):
        return [
            {
                "number": number,
                "hash": f"0x{number}",
                "timestamp": 1_700_000_000 + number,
                "base_fee_per_gas": 100 if number == 10 else 105,
                "gas_used": 10,
                "gas_limit": 20,
                "tx_count": 1,
            }
            for number in block_numbers
        ]


def test_backfill_uses_fee_history_next_base_fee_without_reprojecting(tmp_path: Path) -> None:
    config = BackfillConfig(10, 11, Decimal("3200"), 100, tmp_path, "unused")
    generated = run_backfill(config, client=FakeAlchemy())  # type: ignore[arg-type]
    records = [json.loads(line) for line in generated[0].read_text().splitlines()]
    assert records[0]["data"]["base_fee_per_gas"] == 100
    assert records[0]["data"]["next_base_fee"] == 105
    assert records[1]["data"]["next_base_fee"] == 110
    assert records[1]["data"]["burned_wei"] == 1050


class FakeAlchemyLongo:
    """Responde qualquer faixa, para exercitar o agrupamento em varios lotes."""

    def get_fee_history(self, block_count: int, newest_block: int, reward_percentiles: list[int]):
        assert reward_percentiles == [10, 50, 90]
        return {
            "oldest_block": newest_block - block_count + 1,
            "base_fee_per_gas": [100 + i for i in range(block_count + 1)],
            "gas_used_ratio": [0.5] * block_count,
            "reward": [[1, 2, 3]] * block_count,
        }

    def get_blocks(self, block_numbers: list[int]):
        return [
            {
                "number": number, "hash": f"0x{number}",
                "timestamp": 1_700_000_000 + number,
                "base_fee_per_gas": 100, "gas_used": 10, "gas_limit": 20, "tx_count": 1,
            }
            for number in block_numbers
        ]


def test_um_arquivo_agrupa_varios_lotes_de_rpc(tmp_path: Path) -> None:
    """Lote de RPC e tamanho de arquivo sao independentes.

    O lote precisa ser pequeno (a resposta traz a lista de hashes de cada bloco e
    passa de MB), e o arquivo precisa ser grande (cada arquivo vira um INSERT, e
    muitos INSERTs pequenos levam a TOO_MANY_PARTS no ClickHouse). Amarrar os
    dois num numero so obriga a escolher qual problema ter.
    """
    config = BackfillConfig(1, 50, Decimal("3200"), 10, tmp_path, "unused", blocks_per_file=25)

    generated = run_backfill(config, client=FakeAlchemyLongo())  # type: ignore[arg-type]

    assert len(generated) == 2, "agrupamento nao respeitou blocks_per_file"

    numeros = [
        json.loads(linha)["data"]["block_number"]
        for arquivo in generated
        for linha in arquivo.read_text().splitlines()
    ]
    # A continuidade importa mais que a contagem: buraco no meio de uma serie
    # enviesa o percentil do D-02 sem dar nenhum sinal.
    assert numeros == list(range(1, 51)), "faixa perdeu, duplicou ou desordenou blocos"


def test_intervalo_menor_que_o_arquivo_gera_um_arquivo_so(tmp_path: Path) -> None:
    config = BackfillConfig(1, 5, Decimal("3200"), 10, tmp_path, "unused", blocks_per_file=1800)

    generated = run_backfill(config, client=FakeAlchemyLongo())  # type: ignore[arg-type]

    assert len(generated) == 1
    assert len(generated[0].read_text().strip().splitlines()) == 5
