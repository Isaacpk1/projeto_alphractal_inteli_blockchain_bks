# ETL Python — spool NDJSON para ClickHouse

O ETL implementa exclusivamente o caminho frio. A API .NET calcula as regras de
negocio e fecha arquivos em `spool/ready/`; o Python valida o contrato, insere em
lote no ClickHouse e arquiva o arquivo somente depois da confirmacao duravel.

## Fluxo

```text
spool/ready -> spool/processing -> ClickHouse -> spool/processed
                      | erro de contrato/carga
                      +-----------------------> spool/failed
```

Arquivos deixados em `processing/` por uma queda sao retomados no proximo ciclo.
A entrega e at-least-once; as tabelas de dados usam `ReplacingMergeTree` e os
rollups sao recalculados por bucket, portanto reprocessar um arquivo e seguro.

## Instalar e executar

```bash
cd etl
python -m venv .venv
# Linux/macOS: source .venv/bin/activate
# Windows: .venv\Scripts\activate
python -m pip install -e ".[dev]"
cp .env.example .env

alphractal-etl run          # processa um ciclo
alphractal-etl run --watch  # monitora continuamente
pytest
mypy src
```

O ClickHouse deve estar inicializado pela pasta `infra/`. Os inserts usam
`async_insert=1` e `wait_for_async_insert=1`; um arquivo nunca vai para
`processed/` enquanto o servidor ainda nao confirmou o flush.

## Contrato NDJSON

Cada linha e um objeto independente:

```json
{"table":"eth_blocks","data":{"block_number":123,"block_hash":"0x...","block_timestamp":"2026-08-29T12:00:00Z","base_fee_per_gas":100,"next_base_fee":101,"gas_used":15000000,"gas_limit":30000000,"tx_count":10,"priority_fee_p10":1,"priority_fee_p50":2,"priority_fee_p90":3,"burned_wei":1500000000,"eth_usd":"3200.000000"}}
```

Tabelas aceitas e suas colunas ficam em `src/alphractal_etl/contract.py`. Campo
ausente, campo desconhecido, inteiro negativo ou timestamp sem timezone rejeita o
arquivo inteiro antes de qualquer insert.

## Backfill

Backfill e o unico comando que acessa a Alchemy diretamente. Ele ancora
`eth_feeHistory` em um numero de bloco explicito e usa o valor adicional de
`baseFeePerGas` como taxa do proximo bloco, sem projetar novamente.

```bash
alphractal-etl backfill \
  --from-block 23000000 \
  --to-block 23000100 \
  --eth-usd 3200.00
alphractal-etl run
```

`--eth-usd` e obrigatorio porque gravar preco zero ou inventado corromperia as
metricas financeiras. Para uma carga longa, divida o intervalo por janelas de
preco historico.

## Modulos

- `config.py`: configuracao validada por ambiente.
- `contract.py`: espelho executavel das tabelas de entrada.
- `spool.py`: claim atomico, retomada, archive e quarentena.
- `writer.py`: bulk insert, heartbeat e recomputacao dos rollups afetados.
- `alchemy_client.py`: HTTP com timeout, retry/backoff e erros sem expor a chave.
- `backfill/runner.py`: extracao historica para o mesmo spool do fluxo normal.

O ETL nao calcula faixas de velocidade, congestionamento ou custo por operacao.
Essas regras pertencem a `Services/` da API .NET e chegam prontas no spool.
