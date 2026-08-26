# ClickHouse local — módulo Fees (Alphractal × Inteli Blockchain)

Ambiente de desenvolvimento do banco do projeto *Sistema de monitoramento em tempo
real de custos de taxa na rede Ethereum*. Sobe um ClickHouse em Docker, já com o
schema do módulo Fees criado e, opcionalmente, com 48 h de dados sintéticos para
que API e frontend possam ser construídos **antes** de a chave RPC do parceiro chegar.

> **Por que local e não o ClickHouse deles.** O parceiro respondeu *"salva no banco
> deles, ClickHouse"* (18/08/2026), mas o TAP restringe o projeto a *"ambiente
> isolado"*, sem integração em produção. O caminho seguro é desenvolver contra este
> ClickHouse local com o **mesmo schema** que será proposto a eles, e tratar o
> apontamento para a instância da Alphractal como troca de connection string —
> decisão a confirmar em ata no kick-off de 14/09.

---

## 1. Pré-requisitos

| Item | Mínimo |
|---|---|
| Docker Engine + Compose v2 | `docker compose version` ≥ 2.20 |
| RAM livre | 4 GB (o `config.d/dev.xml` limita o ClickHouse a 60% da RAM do container) |
| Disco | ~2 GB para o volume de dados no dev |

No Windows, rode dentro do **WSL2** — o ClickHouse sofre com I/O em bind mount do
sistema de arquivos do Windows. Os dados aqui ficam em *named volumes*, então isso
já está resolvido, mas o clone do repositório também deve viver no WSL.

## 2. Subir

```bash
cp .env.example .env          # ajuste senhas se quiser
docker compose up -d
docker compose ps             # espere STATUS = healthy (~20-30 s no primeiro start)
```

No primeiro start com o volume vazio, os arquivos de `clickhouse/initdb/` rodam em
ordem e criam banco, tabelas, rollups, views e usuários. **Eles não rodam de novo**
em `docker compose restart` — veja a seção 7 para reaplicar.

## 3. Verificar

```bash
# ping HTTP (é a porta que o .NET usa)
curl http://localhost:8123/ping          # -> Ok.

# 15 objetos criados?
docker compose exec clickhouse clickhouse-client \
  --user alphractal --password alphractal_dev \
  --query "SELECT name, engine FROM system.tables WHERE database='alphractal' ORDER BY name FORMAT PrettyCompact"
```

Interface web (Play): <http://localhost:8123/play> — usuário `alphractal`, senha `alphractal_dev`.

## 4. Popular com dados de desenvolvimento

```bash
docker compose exec -T clickhouse clickhouse-client \
  --user alphractal --password alphractal_dev --multiquery < scripts/seed_dev.sql
```

Gera 14.400 blocos (48 h a 12 s/bloco), 10.800 amostras de mempool e ~14 mil
estimativas de custo. As materialized views preenchem os rollups sozinhas.

```bash
docker compose exec clickhouse clickhouse-client \
  --user alphractal --password alphractal_dev \
  --query "SELECT * FROM alphractal.v_latest_block FORMAT Vertical"
```

> São dados **sintéticos**. Servem para fechar contratos de API, testar gráficos e
> medir custo de query — nunca para conclusão de negócio.

## 5. O que existe dentro do banco

### Caminho quente (o painel ao vivo lê daqui)

| Tabela | Engine | Cadência | Retenção |
|---|---|---|---|
| `eth_blocks` | `ReplacingMergeTree(ingested_at)` | 1 linha por bloco (~12 s) | 180 dias |
| `mempool_samples` | `MergeTree` | 1 a cada 1-2 s | 7 dias |
| `fee_estimates` | `MergeTree` | por bloco × operação × velocidade | 30 dias |
| `eth_usd_prices` | `ReplacingMergeTree` | feed de preço | 90 dias |
| `ingestion_health` | `MergeTree` | heartbeat dos componentes | 14 dias |

`ReplacingMergeTree` em `eth_blocks` existe por causa de **reorg**: reinserir o mesmo
`block_number` sobrescreve em vez de duplicar. É também o que torna o ETL idempotente
— reprocessar uma janela não corrompe o histórico.

### Caminho frio (histórico e agregados — o *"nível diário"* do parceiro)

| Tabela | Alimentada por |
|---|---|
| `eth_fees_1h` | MV `eth_fees_1h_mv` |
| `eth_fees_1d` | MV `eth_fees_1d_mv` |
| `fee_estimates_1d` | MV `fee_estimates_1d_mv` |

São `AggregatingMergeTree` guardando **estados** (`avgState`, `quantilesState`, …),
não números fechados. Isso permite re-agregar 24 buckets horários em um diário sem
o erro clássico de "média de médias".

### Camada de leitura (a API só toca aqui)

`v_latest_block` · `v_mempool_now` · `v_fee_estimates_now` · `v_eth_fees_1h` ·
`v_eth_fees_1d` · `v_fee_estimates_1d` · `v_ingestion_status`

Todas já devolvem **gwei, ETH e USD** — nenhuma conversão de wei em C#, nenhum
`*Merge` e nenhum `FINAL` no código da aplicação. Métrica nova = view nova.

### Usuários

| Usuário | Permissão | Uso |
|---|---|---|
| `alphractal` | admin | migrações, operação local |
| `alphractal_api` | `SELECT` em `alphractal.*` | a API .NET |
| `alphractal_etl` | `SELECT`, `INSERT`, `ALTER`, `OPTIMIZE` | o ETL Python |

`alphractal_etl` já vem com `async_insert = 1`. Isso importa: com um `INSERT` por
bloco e amostras de mempool a cada 1-2 s, sem buffer do lado do servidor o ClickHouse
cria uma *part* minúscula por insert e o merge não acompanha — é o erro nº 1 de quem
usa ClickHouse para streaming.

## 6. Conectar do .NET

```bash
dotnet add package ClickHouse.Client
```

```
Host=localhost;Port=8123;Protocol=http;Database=alphractal;Username=alphractal_api;Password=api_dev_2026;Compression=true
```

Veja `dotnet/appsettings.Development.json` e `dotnet/ClickHouseSnippet.cs` — factory de
conexão, repositório, controller MVC e o endpoint SSE de streaming.

Porta **8123 (HTTP)**, não 9000. A 9000 é o protocolo nativo, usado pelo
`clickhouse-client` e pelo ETL Python (`clickhouse-connect`).

## 7. Comandos do dia a dia

```bash
docker compose logs -f clickhouse          # logs
docker compose stop                        # parar mantendo os dados
docker compose down                        # remover o container, mantendo os dados
docker compose down -v                     # APAGAR TUDO (volumes inclusive)

# reaplicar o schema sem destruir o volume:
for f in clickhouse/initdb/*.sql; do
  docker compose exec -T clickhouse clickhouse-client \
    --user alphractal --password alphractal_dev --multiquery < "$f"
done

# recomeçar do zero:
docker compose down -v && docker compose up -d
```

Os scripts usam `IF NOT EXISTS` / `CREATE OR REPLACE VIEW`, então reaplicar é seguro.

## 8. Problemas comuns

| Sintoma | Causa | Saída |
|---|---|---|
| `Address already in use` na 8123 | outro ClickHouse/serviço na porta | mude `CLICKHOUSE_HTTP_PORT` no `.env` |
| Container reinicia em loop | RAM insuficiente | baixe `max_server_memory_usage_to_ram_ratio` em `config.d/dev.xml` |
| Tabelas não existem após subir | volume já existia; o `initdb` só roda com volume vazio | rode o loop da seção 7 |
| `Authentication failed` no .NET | `005_users.sql` não rodou | confira `CLICKHOUSE_DEFAULT_ACCESS_MANAGEMENT=1` e reaplique o script |
| `Too many parts` no ETL | insert linha a linha sem buffer | use o usuário `alphractal_etl` (async_insert) ou agrupe em lotes |

## 9. Estrutura

```
alphractal-fees-infra/
├── docker-compose.yml
├── .env.example
├── clickhouse/
│   ├── config.d/dev.xml           # limites de memória, log, query_log
│   ├── users.d/dev-limits.xml     # trava de query no perfil default
│   └── initdb/
│       ├── 001_database.sql
│       ├── 002_tables.sql         # caminho quente
│       ├── 003_rollups.sql        # AggregatingMergeTree + MVs
│       ├── 004_views.sql          # camada de leitura da API
│       └── 005_users.sql          # alphractal_api / alphractal_etl
├── scripts/seed_dev.sql           # 48 h de dados sintéticos
└── dotnet/
    ├── appsettings.Development.json
    └── ClickHouseSnippet.cs
```

---

Todo o SQL deste repositório foi executado contra um ClickHouse real (engine 26.7)
antes da entrega: os 5 arquivos rodam limpos e as 7 views retornam dados.
