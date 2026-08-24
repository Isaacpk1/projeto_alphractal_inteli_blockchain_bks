[← Índice](./README.md)

# 09 — Arquitetura e Stack

> **Stack definida pela Alphractal**, não pelo time — em 18/08/2026, ver [10 — Registro de Respostas do Parceiro](./10-registro-respostas-parceiro.md). O TAP deixa a stack livre (*"Frontend: Livre, recomenda-se React + Vite"*, *"Backend: Livre, recomenda-se Node.js"*), então não há conflito com o documento — as recomendações do TAP eram sugestões, não requisitos.

| Camada | Tecnologia |
|---|---|
| Frontend | **React** |
| Backend / API | **.NET** (ASP.NET Core), **estrutura MVC** |
| ETL / tratamento de dados | **Python** |
| Banco analítico | **ClickHouse** |

Como é a infraestrutura real do parceiro, o critério deixa de ser "o que é mais rápido de construir" e passa a ser **"o que eles conseguem absorver"**. Isso aumenta bastante a chance de o código sobreviver ao fim do projeto — que é um dos benefícios declarados no TAP.

**Precedente do parceiro.** A Alphractal já opera um pipeline próprio no padrão *ingestão on-chain → tratamento dos dados → API → front-end* (hoje aplicado a Dogecoin). A divisão em dois caminhos descrita abaixo **não é proposta nossa — é o padrão interno deles**, aplicado ao Ethereum. Isso encerra as dúvidas 24 e 25.

---

## 1. Divisão em dois caminhos

O erro que arruinaria este projeto seria colocar Python e ClickHouse no meio do caminho do tempo real. Cada salto adicional consome o orçamento de 2 segundos do RNF-01, e o ClickHouse é um banco **analítico** — ele é ótimo para varrer milhões de linhas e péssimo para responder uma linha por vez a cada 12 segundos.

Por isso a arquitetura se divide em dois caminhos independentes:

```
                          ┌──────────────────────────────────────────┐
   Alchemy (WebSocket)    │  CAMINHO QUENTE  —  alvo: < 2 s          │
   newHeads ─────────────▶│  .NET / ASP.NET Core                     │
   eth_feeHistory ───────▶│   • ingestão + reconexão (Nethereum)     │
                          │   • cálculo das regras RN-01 a RN-05     │
                          │   • janela de 300 blocos EM MEMÓRIA      │
                          │   • fan-out via Channel<T>               │
                          └───────────┬──────────────────┬───────────┘
                                      │ SSE              │ spool NDJSON
                                      ▼                  ▼
                          ┌───────────────────┐  ┌──────────────────────────┐
                          │  React (painel)   │  │  CAMINHO FRIO            │
                          └───────────────────┘  │  Python ETL              │
                                      ▲          │   • lê lotes do spool    │
                                      │          │   • backfill histórico   │
                    consultas         │          │   • bulk insert          │
                    históricas        │          └────────────┬─────────────┘
                          ┌───────────┴───────┐               ▼
                          │  .NET  /api/...   │◀────────  ClickHouse
                          └───────────────────┘         (materialized views)
```

**Caminho quente** — tudo que o painel mostra ao vivo. Nunca toca em Python nem em ClickHouse.
**Caminho frio** — histórico, agregações, percentis e heatmap. Latência de minutos é aceitável.

Isso reforça a **RN-14** (o banco não serve tempo real), que já estava na especificação e agora vira estrutural.

---

## 2. Responsabilidades por componente

### .NET — ASP.NET Core

| Responsabilidade | Implementação |
|---|---|
| Ingestão RPC | **Nethereum** (`StreamingWebSocketClient` + `EthNewBlockHeadersObservableSubscription`) dentro de um `BackgroundService` |
| Regras de negócio | RN-01 a RN-05, usando `System.Numerics.BigInteger` |
| Janela quente | Ring buffer em memória, 300 blocos (RN-10) |
| Fan-out para N clientes | `System.Threading.Channels.Channel<T>` — um produtor (RPC), N consumidores (SSE) |
| Transporte ao painel | SSE em **controller MVC**, action retornando `IAsyncEnumerable<T>` com `Content-Type: text/event-stream` |
| Consultas históricas | Leitura no ClickHouse via `ClickHouse.Client` |
| Spool para o ETL | Escrita append-only de NDJSON, fora do caminho crítico (RNF-25) |

> **Ganho real da troca de Node para .NET:** `BigInteger` é nativo. O risco R-03 (perda de precisão em wei por causa do `number` de 64 bits do JavaScript) **deixa de existir**. Era o risco mais insidioso da especificação anterior — silencioso, e apareceria justamente na demo. A Nethereum já trabalha com `BigInteger` de ponta a ponta.

> **Custo real da troca:** a Nethereum é madura, mas o ecossistema Web3 documenta quase tudo em `viem`/`ethers`. Cada problema levará mais tempo para resolver por falta de exemplos. Em duas semanas efetivas de código, isso é risco de cronograma (ver R-13).

### Python — ETL

| Responsabilidade | Observação |
|---|---|
| Consumir os lotes do spool e carregar no ClickHouse | `clickhouse-connect`, inserts em lote |
| Backfill histórico | Popular 30 dias de blocos para habilitar D-02 e D-04 |
| Enriquecimento | Cotação ETH/USD histórica, rótulos de contratos (D-06) |
| Validação e qualidade | Detectar lacunas de blocos, reconciliar contra o RPC |

**Python fica fora do caminho crítico por decisão de arquitetura**, não por limitação da linguagem.

### Estrutura MVC — como as camadas se encaixam

O parceiro pediu **estrutura MVC** no .NET. Isso não conflita com a arquitetura em camadas da **RNF-14**: MVC define como o *transporte* é organizado, e as camadas definem para onde vai a lógica. O mapeamento é direto:

| Camada da RNF-14 | Onde vive em MVC | Regra |
|---|---|---|
| *Transport* | `Controllers/` — `FeesController` (SSE, snapshot, histórico), `HealthController` | Nenhum cálculo. Só orquestra e serializa |
| *Service* | `Services/` — regras RN-01 a RN-05 | Onde vive **toda** a matemática (RN-09) |
| *Repository* | `Repositories/` — leitura no ClickHouse, spool NDJSON | Trocável sem tocar em Service |
| *Provider* | `Providers/` — Nethereum, cotação ETH/USD | Trocável sem tocar em Service |
| *Model* | `Models/` — DTOs do contrato SSE, entidades de bloco | Fonte única do contrato (RNF-13) |

A ingestão contínua não é um controller: vive em `BackgroundServices/BlockIngestionService`, registrado como `IHostedService`. O "V" de MVC é servido pelo **React** — o .NET não renderiza views, expõe JSON e SSE. Vale dizer isso explicitamente na banca, porque "MVC sem view" é pergunta previsível.

### Handoff .NET → Python: spool de arquivos

Avaliamos fila (RabbitMQ/Redis) e escrita direta do .NET no ClickHouse. Para um MVP de 4 semanas, o **spool em disco** ganha:

1. **Zero infraestrutura adicional** — sem mais um serviço para subir, configurar e explicar na banca.
2. **Resolve o problema de lote do ClickHouse de graça** — o arquivo já agrupa naturalmente (ver seção 3).
3. **Sobrevive a queda de qualquer lado** — se o Python cair, os arquivos se acumulam e são processados depois. Se o .NET cair, o Python continua drenando o que já existe.
4. **É literalmente um ETL** — extract (arquivo), transform (Python), load (ClickHouse). Fácil de defender e de desenhar no slide.

**Convenção:** `.NET` escreve `spool/pending/blocks-YYYYMMDD-HHMM.ndjson`, fecha o arquivo ao virar o minuto e move para `spool/ready/`. O Python processa apenas o que está em `ready/` e move para `processed/` após confirmação do insert. Um arquivo por minuto ≈ 5 blocos por lote.

---

## 3. ClickHouse — o que muda em relação a um banco relacional

Detalhamento completo em [04 — Persistência](./04-persistencia-banco-de-dados.md). Os três pontos que mais causam retrabalho:

**1. Nunca inserir linha a linha.** Cada `INSERT` cria um *part* no disco, e um processo em segundo plano os funde. Inserindo um bloco a cada 12 s você gera 7.200 parts/dia e o merge não acompanha — a tabela trava com `TOO_MANY_PARTS`. É o erro nº 1 de quem chega ao ClickHouse vindo de Postgres. A arquitetura de spool acima já resolve; se por algum motivo for preciso inserir direto, usar `async_insert=1` ou uma tabela `Buffer`.

**2. `PRIMARY KEY` não impede duplicata.** No ClickHouse a chave primária é um **índice esparso**, não uma restrição de unicidade. Inserir o mesmo bloco duas vezes gera duas linhas. Idempotência (RN-08) exige `ReplacingMergeTree` com coluna de versão — e a deduplicação é **assíncrona**, acontece só na fusão dos parts. Consultas que precisam de garantia usam `FINAL` ou `argMax()`.

**3. Não existe `UPDATE` barato.** `ALTER TABLE ... UPDATE` é uma *mutation*: assíncrona, pesada, reescreve partes inteiras. Tratamento de reorg (RN-16) passa a ser "inserir nova versão", nunca "atualizar a linha".

**Em compensação, três coisas ficam melhores:**

- **Tipos nativos para wei** (`UInt64`, `UInt256`) — some a gambiarra de guardar wei como `TEXT`.
- **Retenção declarativa** — `TTL block_time + INTERVAL 30 DAY` na definição da tabela. A RN-15 vira configuração, não job.
- **Agregação declarativa** — *materialized views* com `AggregatingMergeTree` mantêm os rollups horários automaticamente. O RF-37 deixa de ser código. E `quantile()` nativo torna **D-02 (percentil) e D-04 (heatmap) quase triviais** — eram os diferenciais mais caros da lista.

**Honestidade sobre dimensionamento:** 216 mil linhas por mês. ClickHouse é projetado para bilhões. Tecnicamente é desproporcional — mas o critério aqui é alinhamento com a infraestrutura do parceiro, e o ganho em D-02/D-04 é real.

---

## 4. Linha de corte do MVP

Quatro tecnologias e quatro runtimes (TypeScript, C#, Python, SQL) em ~2 semanas efetivas de código é o maior risco do projeto. A mitigação é definir desde já o que é indispensável:

| Camada | Status | Consequência se atrasar |
|---|---|---|
| **.NET + React (caminho quente)** | 🔴 Indispensável | Sem isso não há demo. É o projeto. |
| **ClickHouse + Python (caminho frio)** | 🟡 Importante | O painel ao vivo continua funcionando; perdem-se D-02, D-04 e o histórico longo |

**Regra:** o painel ao vivo precisa estar demonstrável **sem** ClickHouse e sem Python. Se a semana 3 apertar, corta-se o caminho frio inteiro sem comprometer a entrega principal.

---

## 5. Impactos nos requisitos existentes

| Requisito | Mudança |
|---|---|
| **RN-06** (precisão) | `System.Numerics.BigInteger` no .NET; `UInt256`/`UInt64` no ClickHouse. Some a regra de guardar wei como `TEXT` |
| **RN-08 / RN-16** (reorg) | De `UPSERT` para `ReplacingMergeTree` + coluna de versão, com dedup assíncrona |
| **RN-15** (retenção) | Vira `TTL` declarativo na tabela |
| **RF-37** (agregados) | Vira *materialized view*, não código |
| **RNF-13** (tipagem) | `TypeScript strict` no front; `<Nullable>enable</Nullable>` + warnings como erro no .NET; `mypy` no Python |
| **RNF-22** (execução local) | `docker-compose` deixa de ser opcional — ClickHouse exige container |
| **R-03** (precisão float) | **Risco praticamente eliminado** pelo `BigInteger` nativo |
| **D-02 / D-04** | Esforço cai bastante — `quantile()` nativo e materialized views |

## 6. Dúvidas relacionadas a esta seção

> A numeração canônica é a do [06 — Dúvidas](./06-duvidas-kickoff.md). Esta seção apenas aponta para lá.

| # | Assunto | Estado |
|---|---|---|
| 12 | SSE vs **SignalR** — o TAP recomenda SSE; SignalR é o idiomático em .NET | Aberta |
| 21 | Instância de **ClickHouse**: deles ou Docker local? | Aberta — ver §7 |
| 22 | Versão e convenções de **.NET** / template MVC | Parcial — MVC confirmado, resto aberto |
| 23 | **Nethereum**: já usam? Há código de referência? | Aberta — maior risco de cronograma (R-13) |
| 24 | Padrão de **ETL em Python** (orquestrador, convenções) | Encerrada — pipeline próprio, ver §1 |
| 25 | Divisão **.NET × Python** | Encerrada — confirma esta arquitetura |
| 28 | Convenções específicas de MVC (pastas, DI, validação) | Nova |

## 7. Instância de ClickHouse — decisão provisória

O parceiro disse *"salva no banco deles, ClickHouse"*. Gravar diretamente na instância de produção da Alphractal **colide com a restrição do TAP** (*"sem integração direta em produção"*, *"protótipo operando em ambiente isolado"*).

**Decisão adotada até o kick-off:** ClickHouse **local via Docker**, com schema espelhando o padrão de nomes deles. Ao fim do projeto, a carga na instância real é uma troca de string de conexão — que fica a critério da Alphractal executar.

Isso preserva a restrição contratual sem perder o alinhamento. Confirmar na **dúvida 21**.
