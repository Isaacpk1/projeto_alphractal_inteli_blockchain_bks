# api/ — API .NET 10 (MVC)

**Dono:** você. **Ingere:** Ethereum via Nethereum (WebSocket). **Serve:** SSE e JSON
para o painel em [`web/`](../web/README.md). **Consulta:** ClickHouse — somente para histórico.

Ainda não implementada. A estrutura abaixo é o acordo de onde cada coisa mora, e
espelha o mapeamento de camadas de
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

A conexão contínua com a Alchemy vive em `BackgroundServices/BlockIngestionService`,
registrado como `IHostedService`. Ela é a **única** conexão RPC do projeto: o ETL
lê o spool NDJSON que esta API escreve, não a rede. Duas conexões para a mesma
fonte dobrariam o consumo do [orçamento de RPC](../docs/requisitos/08-orcamento-rpc.md).

## Sem `Views/` Razor

Estrutura MVC aqui = `Controllers/` + `Models/`. O "V" é o painel React, que é
projeto separado, em [`web/`](../web/README.md). A View não foi eliminada —
mudou de processo: antes rodaria no servidor, agora roda no navegador.

Decisão registrada em [`docs/adr/001-mvc-sem-views.md`](../docs/adr/001-mvc-sem-views.md),
**a confirmar em ata no kick-off de 14/09** — a frase do parceiro não especificou
qual dos dois templates do ASP.NET ele quis dizer.

Cada pasta de `src/Alphractal.Fees.Api/` tem um `README.md` com as regras dela.

## Rodar (quando existir)

```bash
cd api
dotnet run --project src/Alphractal.Fees.Api
curl http://localhost:5080/api/v1/health
```

O projeto de testes ainda não foi criado — `dotnet new xunit -o tests/Alphractal.Fees.Tests`
e adicionar ao `Alphractal.Fees.slnx`.

`Directory.Build.props` fixa `net10.0`, `Nullable=enable` e warnings como erro
para todos os projetos da pasta (RNF-13).
