# api/ — API .NET 10 (MVC)

**Dono:** back-end. **Ingere:** Ethereum via Nethereum (WebSocket + HTTP). **Serve:** SSE e JSON
para o painel em [`web/`](../web/README.md). **Consulta:** ClickHouse — somente para histórico.
A estrutura abaixo espelha o mapeamento de camadas de
[`docs/requisitos/09-arquitetura-e-stack.md §2`](../docs/requisitos/09-arquitetura-e-stack.md).

```
src/Alphractal.Fees.Api/
├── Program.cs              composição: options, CORS, controllers, hosted services
├── Configuration/          ClickHouseOptions, FeesOptions (padrão Options)
├── Controllers/            transporte: rotas, validação, status HTTP. Zero cálculo
├── Models/
│   ├── Domain/             entidades de bloco e da janela quente
│   └── Responses/          DTOs do contrato SSE e REST (fonte única — RNF-13)
├── Services/               RN-01 a RN-05, janela de 300 blocos, broadcaster SSE
├── Repositories/           leitura no ClickHouse + escrita do spool NDJSON
│   └── Sql/                as queries, isoladas do C#
├── Providers/              Nethereum, cotação ETH/USD
└── BackgroundServices/     BlockIngestionService (IHostedService da ingestão)
tests/Alphractal.Fees.Tests/
```

## As três regras que definem esta pasta

**1. O caminho quente nunca toca o ClickHouse.** É a RN-14, e é estrutural.
O SSE é alimentado pela janela de 300 blocos **em memória**, preenchida pelo
`BlockIngestionService` e distribuída por `Channel<T>` — um produtor (RPC),
N consumidores (SSE). Servir o SSE a partir de consulta ao banco entregaria dado
com frescor de ~1 minuto e tornaria o RNF-01 (< 2 s) impossível por construção.

**2. O ClickHouse só aparece no histórico.** `Repositories/` existe para
`/api/history` e para os agregados do caminho frio. Latência de minutos é aceitável ali.

**3. Toda a matemática vive em `Services/`.** RN-09. Controller não calcula, e
`System.Numerics.BigInteger` é o tipo de qualquer valor em wei — nunca `double`,
nunca `long`. É o que elimina o risco R-03.

## Ingestão não é controller

A assinatura contínua `newHeads` vive em `BackgroundServices/BlockIngestionService`,
registrado como `IHostedService`. Para cada bloco, o mesmo serviço usa HTTP para
`eth_feeHistory`, `eth_getBlockReceipts` e contingência. Os recibos fornecem o
total efetivamente pago e a contagem de transações. O ETL só acessa o RPC no
comando explícito de backfill; no serviço contínuo ele apenas drena o spool.

## Sem `Views/` Razor

Estrutura MVC aqui = `Controllers/` + `Models/`. O "V" é o painel React, que é
projeto separado, em [`web/`](../web/README.md). A View não foi eliminada —
mudou de processo: antes rodaria no servidor, agora roda no navegador.

Decisão registrada em [`docs/adr/001-mvc-sem-views.md`](../docs/adr/001-mvc-sem-views.md),
**a confirmar em ata no kick-off de 14/09** — a frase do parceiro não especificou
qual dos dois templates do ASP.NET ele quis dizer.

Cada pasta de `src/Alphractal.Fees.Api/` tem um `README.md` com as regras dela.

## Endpoints implementados

A API conecta no ClickHouse como `alphractal_api` (SELECT apenas nas views
`v_*`) e mantém o caminho quente em memória:

| Rota | Serve |
|---|---|
| `GET /api/v1/health` | liveness do processo |
| `GET /api/v1/status` | saude da ingestao, de `v_ingestion_status` |
| `GET /api/v1/fees/latest` | ultimo bloco **do banco** — diagnostico/fallback, nao e o dado ao vivo |
| `GET /api/v1/fees/mempool` | ultima amostra de mempool |
| `GET /api/v1/fees/estimates` | estimativa atual por operacao/velocidade |
| `GET /api/v1/fees/history?granularity=hour\|day&from=&to=&limit=` | serie do rollup |
| `GET /api/v1/fees/estimates/history?from=&to=&limit=` | custo diario por operacao (D-04) |
| `GET /api/v1/fees/percentile` | posição da base fee atual no histórico |
| `GET /api/v1/fees/planejamento` | melhor janela horária segundo o histórico |
| `GET /api/v1/fees/heatmap` | agregado dia-da-semana × hora |
| `GET /api/v1/fees/eth-usd` | cotação atual e variação de 24 h |
| `GET /api/v1/fees/snapshot` | último snapshot do caminho quente |
| `GET /api/v1/fees/stream` | SSE de blocos e estimativas ao vivo |
| `GET /api/v1/fees/price-stream` | SSE da cotacao ETH/USD a cada mudanca no ticker da Coinbase |
| `GET /api/v1/fees/custo` | custo por operação para um preço de gas |
| `GET /api/v1/fees/queima` | taxa de queima observada na janela quente |
| `GET /api/v1/fees/window` | janela recente de blocos em memória |

ClickHouse fora do ar responde **503 com ProblemDetails**, nunca 500: e estado
previsto: o painel ao vivo nao depende do caminho frio (doc 09 secao 4).

## Rodar

```bash
# 1. Infra, migrations, API e ETL
cd infra
docker compose up -d --build

# 2. segredos: copie o exemplo e preencha a chave da Alchemy
cd ../api
copy .env.example .env      # Windows  (Linux/macOS: cp .env.example .env)

# 3. alternativa ao container: subir a API no host
dotnet run --project src/Alphractal.Fees.Api
curl http://localhost:5080/api/v1/health
curl http://localhost:5080/api/v1/status
curl "http://localhost:5080/api/v1/fees/history?granularity=hour&limit=24"
```

## Segredos

Mesmo mecanismo do `etl/` e do `infra/`: um arquivo `.env` na pasta, ignorado
pelo Git. Copie de `.env.example`.

**O separador de secao e DOIS underlines** — `Fees__RpcHttpUrl` corresponde a
`"Fees": { "RpcHttpUrl": ... }` do `appsettings.json`. Um underline so cria uma
chave diferente e a configuracao nao chega no lugar esperado.

Precedencia, do mais forte para o mais fraco: variavel de ambiente do processo,
depois `.env`, depois `appsettings.{Environment}.json`, depois `appsettings.json`.
Container e CI sobrepoem sem editar arquivo nenhum.

O `.env` **nao** vai para o Git (`.gitignore` da raiz), mas ele mora dentro da
pasta do projeto: nao inclua o arquivo preenchido ao compactar o repositorio para
entrega, e rotacione a chave se ela vazar.

`Fees__RpcWebSocketUrl` **nao** e obrigatoria no boot: os dois caminhos precisam
subir separados. Sem ela a API sobe, serve o caminho frio e registra um aviso; a
ingestao fica desligada.

Sem dado no banco, `/api/v1/fees/latest` responde 404 com a instrucao de rodar o
backfill. Para popular:

```bash
cd etl && alphractal-etl backfill --from-block 23000000 --to-block 23000100 --eth-usd 3200.00
alphractal-etl run
```

O backfill usa recibos por padrão (`--recibos-por-lote 8`) para preencher
`total_fee_wei`. Use `0` somente quando aceitar que Total Fees fique indisponível
ou estimado naquele intervalo. A decisão está na
[ADR-004](../docs/adr/004-total-de-taxas-vem-do-recibo.md).

## Convencoes desta pasta

- **SQL nao mora em `.cs`.** Um `.sql` por consulta em `Repositories/Sql/`,
  embutido no assembly pelo glob do `.csproj` e carregado por `SqlResources`.
- **Parametro e do lado do servidor** (`{nome:Tipo}`). Nada de concatenacao.
- **Repositorio devolve `Models/Domain/ColdPath/`**, nao `Models/Responses/`.
  O controller faz a projecao. E o que permite o front pedir outro formato sem
  mexer em consulta.
- **Nenhuma conversao de unidade em C#**: as views ja entregam gwei, ETH e USD.

Os testes vivem em `tests/Alphractal.Fees.Tests/` e rodam com:

```bash
dotnet test Alphractal.Fees.slnx
```

`Directory.Build.props` fixa `net10.0`, `Nullable=enable` e warnings como erro
para todos os projetos da pasta (RNF-13).
