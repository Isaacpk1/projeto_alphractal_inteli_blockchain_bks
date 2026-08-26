# etl/ — ingestão Ethereum → ClickHouse

**Dono:** o dev responsável pela ingestão. **Escreve em:** camada 1 do ClickHouse
(`eth_blocks`, `mempool_samples`, `fee_estimates`, `eth_usd_prices`, `ingestion_health`).

> **Leia primeiro:** [`docs/requisitos/09-arquitetura-e-stack.md §2`](../docs/requisitos/09-arquitetura-e-stack.md)
> e [`04-persistencia-banco-de-dados.md`](../docs/requisitos/04-persistencia-banco-de-dados.md).
> São o acordo com a API. Coluna, unidade e cadência estão lá, e mudar qualquer um
> dos três quebra o painel silenciosamente — sem erro, só com número errado.

```
src/alphractal_etl/
├── contract.py       espelho em Python do 002_tables.sql — nomes, tipos, unidades
├── spool.py          leitura dos lotes NDJSON que a API .NET escreve em ready/
├── transform/        linha do spool → linha pronta para o ClickHouse
├── backfill/         carga histórica de 30 dias (habilita D-02 e D-04)
└── writer.py         escrita em lote no ClickHouse
tests/
└── test_contract.py  valida uma linha contra o contrato SEM precisar de banco
```

## As quatro regras que não podem ser quebradas

1. **Unidade é wei**, inteiro, em todas as colunas de taxa. Nunca gwei, nunca float.
2. **Timestamp é UTC.** `block_timestamp` vem da rede, não do relógio da máquina.
3. **Escreva com o usuário `alphractal_etl`.** Ele tem `async_insert` ligado — sem
   isso, um INSERT por bloco gera uma *part* por INSERT e o servidor trava com
   `Too many parts` em poucas horas.
4. **Reinserir o mesmo bloco é seguro e esperado.** `eth_blocks` é
   `ReplacingMergeTree` com chave `block_number`: reorg e reprocessamento sobrescrevem
   em vez de duplicar. Idempotência é responsabilidade do schema, não sua.

E uma quinta, que é a que salva o painel: **escreva o heartbeat em
`ingestion_health`** a cada ciclo. É o único jeito de a tela conseguir dizer
"dados atrasados há 40 s" em vez de mostrar um número velho como se fosse atual.

## Ambiente

```bash
cd etl
python -m venv .venv && source .venv/bin/activate
pip install -r requirements.txt
cp .env.example .env      # SPOOL_PATH, ALCHEMY_HTTP_URL (só backfill), CLICKHOUSE_*
```

O ClickHouse de destino é o de `infra/` — mesmo schema que a Alphractal receberá.

> **A ingestão ao vivo não é sua.** Quem mantém a conexão WebSocket com a Alchemy
> é a API .NET (`api/src/Alphractal.Fees.Api/BackgroundServices/`). Você lê os
> lotes NDJSON que ela deixa em `spool/ready/` e move para `processed/` só depois
> do insert confirmado. `ALCHEMY_HTTP_URL` serve apenas ao backfill histórico e à
> reconciliação de lacunas — não abra uma segunda assinatura de `newHeads`.
