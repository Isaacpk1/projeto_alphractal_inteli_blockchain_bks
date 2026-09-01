# ClickHouse local — módulo Fees

Ambiente analítico do caminho frio. O Compose sobe ClickHouse e o worker ETL;
o painel ao vivo não consulta este banco. A API .NET lê somente as views `v_*`
para histórico e status da ingestão.

## Subir

```bash
cd infra
cp .env.example .env
docker compose up -d
docker compose ps
```

Os scripts de `clickhouse/initdb/` rodam em ordem somente quando o volume está
vazio. As mudanças compatíveis e idempotentes ficam em `scripts/migrate_*.sql`:
o serviço one-shot `clickhouse-migrate` reaplica esses arquivos, em ordem, a cada
`docker compose up` e a API só inicia depois que eles terminam. Assim, uma view
nova também é criada em volumes existentes sem apagar dados.

Para uma mudança incompatível localmente, use `docker compose down -v` e suba
novamente; esse comando apaga o volume local do Compose.

## Estrutura

- `001_database.sql`: banco `alphractal`.
- `002_tables.sql`: cinco tabelas de entrada do ETL.
- `003_rollups.sql`: rollups horários/diários idempotentes.
- `004_views.sql`: sete contratos de leitura da API.
- `005_users.sql`: usuários com privilégio mínimo.
- `scripts/migrate_*.sql`: evolução idempotente de volumes já inicializados.
- `scripts/seed_dev.sql`: 48 horas de dados sintéticos e rollups.

## Idempotência

`eth_blocks`, `mempool_samples`, `fee_estimates` e `eth_usd_prices` usam
`ReplacingMergeTree(ingested_at)` com uma chave lógica no `ORDER BY`. O ETL pode
repetir um lote após falha sem criar uma segunda versão lógica nas consultas com
`FINAL`.

Materialized views incrementais não são usadas: elas processariam novamente um
bloco reinserido antes de o `ReplacingMergeTree` deduplicá-lo. Após cada lote o
ETL recalcula somente as horas/dias afetados e grava uma nova versão em
`eth_fees_rollup` e `fee_estimates_1d`.

## Durabilidade e acesso

O usuário ETL usa `async_insert=1` com `wait_for_async_insert=1`. O servidor
agrega inserts pequenos, mas só confirma depois do flush; apenas então o arquivo
vai para `processed/`.

- `alphractal_api`: `SELECT` somente nas sete views, executadas com
  `SQL SECURITY DEFINER`.
- `alphractal_etl`: `SELECT/INSERT` somente nas tabelas de carga e rollup.
- `alphractal`: administração local e migrations.

As senhas são defaults locais. Em outro ambiente, aplique migrations próprias e
injete credenciais por secret/env.

## Popular e verificar

```bash
docker compose exec -T clickhouse clickhouse-client \
  --user alphractal --password alphractal_dev --multiquery \
  < scripts/seed_dev.sql

docker compose exec clickhouse clickhouse-client \
  --user alphractal --password alphractal_dev \
  --query "SELECT * FROM alphractal.v_latest_block FORMAT Vertical"

docker compose exec clickhouse clickhouse-client \
  --user alphractal --password alphractal_dev \
  --query "SELECT count() FROM alphractal.v_eth_fees_1h"
```

> **PowerShell:** `<` e operador reservado — a linha do seed falha com
> `RedirectionNotSupported`. Rode essa linha pelo `cmd`, que passa os bytes crus:
>
> ```powershell
> cmd /c "docker compose exec -T clickhouse clickhouse-client --user alphractal --password alphractal_dev --multiquery < scripts\seed_dev.sql"
> ```
>
> Nao use `Get-Content ... |` no lugar: o `seed_dev.sql` tem acentos e o pipe do
> PowerShell reencoda o conteudo no caminho.

Sucesso e **silencioso** — o `clickhouse-client` nao imprime nada em `INSERT` que
funcionou. Confirme com a contagem:

```bash
docker compose exec clickhouse clickhouse-client \
  --user alphractal --password alphractal_dev \
  --query "SELECT count() FROM alphractal.eth_blocks"   # 14400
```

Os dados do seed são sintéticos e não servem para análise de negócio.

### Calibrar o Total Fees legado de 25–30/08/2026

Esses dias foram coletados antes de `total_fee_wei` existir. Para reproduzir no
gráfico os agregados públicos `FeeTotNtv` usados como referência pela Alphractal,
sem baixar novamente os recibos de ~43 mil blocos, aplique a carga idempotente:

```powershell
cmd /c "docker compose exec -T clickhouse clickhouse-client --user alphractal --password alphractal_dev --multiquery < scripts\load_reference_total_fees_2026_08.sql"
```

A carga substitui somente os seis rollups diários; blocos brutos, buckets
horários e a coleta nova por recibos não são alterados.

## Retenção

- blocos e estimativas: 30 dias;
- mempool: 7 dias;
- preços ETH/USD: 90 dias;
- heartbeat: 14 dias;
- rollups: sem TTL, preservados para histórico longo.
