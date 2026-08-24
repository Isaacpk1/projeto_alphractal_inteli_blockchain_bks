[← Índice](./README.md)

# 01 — Requisitos Funcionais (RF)

RF-01 a RF-40. Prioridade: **[M]** Must · **[S]** Should · **[C]** Could

---

## 1. Ingestão de dados on-chain (backend)

| ID | Requisito | Prio |
|---|---|---|
| RF-01 | O sistema deve manter uma conexão WebSocket persistente com um provedor RPC Ethereum e se inscrever no evento de novos blocos (`newHeads`). | M |
| RF-02 | A cada novo bloco, o sistema deve extrair `number`, `timestamp`, `baseFeePerGas`, `gasUsed` e `gasLimit`. | M |
| RF-03 | O sistema deve consultar `eth_feeHistory` para derivar percentis de *priority fee* (gorjeta) dos últimos **`N_fee` = 20** blocos (RN-10). | M |
| RF-04 | O sistema deve reconectar automaticamente ao RPC com *backoff* exponencial em caso de queda, sem derrubar os clientes conectados. | M |
| RF-05 | O sistema deve possuir um modo de contingência por *polling* HTTP (`eth_blockNumber` / `eth_getBlockByNumber`) caso o WebSocket fique indisponível. | S |
| RF-06 | O sistema deve suportar um provedor RPC secundário (failover) configurável por variável de ambiente. | C |
| RF-07 | O sistema deve coletar métricas de mempool (nº de transações pendentes e *gas price* médio das pendentes). | C |

> ⚠️ **RF-07** depende da dúvida nº 3 do kick-off. A subscription não é bloqueada por plano, mas é cobrada por byte entregue: no plano gratuito da Alchemy ela esgota a cota mensal em ~7 dias (só hashes) ou em menos de 24 h (objetos completos). Ver [08 — Orçamento RPC](./08-orcamento-rpc.md). Manter como *Could* salvo confirmação de plano pago.

## 2. Cálculo e enriquecimento

| ID | Requisito | Prio |
|---|---|---|
| RF-08 | O sistema deve calcular três faixas de taxa — **Lento / Padrão / Rápido** — combinando `baseFee` + percentis de *priority fee*. | M |
| RF-09 | O sistema deve converter o custo estimado de gwei para **ETH** e para **USD**. | M |
| RF-10 | O sistema deve obter a cotação ETH/USD de uma fonte externa, com cache e atualização periódica. | M |
| RF-11 | O sistema deve estimar o custo de tipos de transação pré-definidos (transferência ETH, transferência ERC-20, swap DEX, aprovação, mint NFT) usando *gas limits* de referência configuráveis. | M |
| RF-12 | O sistema deve calcular um **índice de congestionamento / "saúde da rede"** comparando a base fee atual com a média móvel dos últimos **`N_cong` = 100** blocos (RN-04, RN-10). | M |
| RF-13 | O sistema deve projetar a `baseFee` do próximo bloco a partir da regra do EIP-1559 (variação de até ±12,5% conforme `gasUsed`/`gasLimit`). | S |
| RF-14 | O sistema deve manter um histórico em memória (janela deslizante) dos últimos **`N_buffer` = 300** blocos para alimentar gráficos (RN-10). | M |
| RF-15 | O sistema deve persistir o histórico em banco de dados — detalhado em [04 — Persistência](./04-persistencia-banco-de-dados.md). | S |

## 3. Exposição / API (backend)

| ID | Requisito | Prio |
|---|---|---|
| RF-16 | O backend deve expor um endpoint **SSE** (`GET /api/fees/stream`) que emite um evento a cada novo bloco, servido por um controller MVC (RNF-31). | M |
| RF-17 | O backend deve expor um endpoint REST de *snapshot* (`GET /api/fees/current`) para hidratar a UI no carregamento inicial. | M |
| RF-18 | O backend deve expor o histórico recente (`GET /api/fees/history?blocks=N`) para o gráfico. | M |
| RF-19 | O backend deve fazer *fan-out*: uma única conexão RPC alimenta todos os clientes SSE conectados. | M |
| RF-20 | O backend deve expor `GET /health` com estado da conexão RPC, último bloco recebido e *uptime*. | S |
| RF-21 | O backend deve enviar *heartbeat* periódico no canal SSE para evitar timeout de proxies e balanceadores. | S |

## 4. Interface — painel da aba "Fees" (frontend)

| ID | Requisito | Prio |
|---|---|---|
| RF-22 | O painel deve exibir, em tempo real, os cards das três faixas de taxa (Lento/Padrão/Rápido) em gwei e USD. | M |
| RF-23 | O painel deve exibir um indicador visual do nível de congestionamento da rede (Baixo / Normal / Alto / Extremo). | M |
| RF-24 | O painel deve exibir um gráfico em tempo real da base fee dos últimos **`N_buffer` = 300** blocos. | M |
| RF-25 | O painel deve exibir o número do último bloco e o tempo decorrido desde seu recebimento ("há 4s"). | M |
| RF-26 | O painel deve exibir o status da conexão (Ao vivo / Reconectando / Offline / Dados desatualizados). | M |
| RF-27 | O usuário deve poder selecionar o tipo de transação e ver o custo estimado correspondente. | M |
| RF-28 | O usuário deve poder alternar a unidade exibida (gwei ↔ USD ↔ ETH). | S |
| RF-29 | O painel deve indicar visualmente a direção da variação da taxa (subindo/caindo) em relação ao bloco anterior. | S |
| RF-30 | O usuário deve poder definir um limiar de gas e receber alerta visual quando a taxa cruzar esse valor. | C |
| RF-31 | O painel deve ser responsivo e seguir a identidade visual (tema escuro) da Alphractal. | M |
| RF-32 | O painel deve exibir estados de carregamento, vazio e erro de forma explícita. | S |
| RF-33 | O frontend deve reconectar automaticamente ao SSE após perda de conexão. | M |

## 5. Persistência e ETL

*(detalhamento em [04 — Persistência (ClickHouse)](./04-persistencia-banco-de-dados.md) e [09 — Arquitetura](./09-arquitetura-e-stack.md))*

> Toda esta seção pertence ao **caminho frio**. Nenhum item aqui pode ser pré-requisito do painel ao vivo — ver a linha de corte em [09 §4](./09-arquitetura-e-stack.md).

| ID | Requisito | Prio |
|---|---|---|
| RF-34 | O .NET deve gravar cada bloco processado no spool NDJSON, e o ETL Python deve carregá-lo no ClickHouse **em lote** (~25 registros ou 300 s por `INSERT`) — nunca linha a linha. | S |
| RF-35 | Ao iniciar, o .NET deve recarregar a janela quente consultando o ClickHouse com `FINAL`/`argMax()`, para que o painel abra já com o gráfico populado. | S |
| RF-36 | O ETL deve oferecer um script de *backfill* sob demanda, detectando lacunas de blocos e buscando-as via RPC, com controle explícito de consumo de CU. | C |
| RF-37 | Agregados **horários e diários** devem ser mantidos por **materialized views** (`AggregatingMergeTree`), e a retenção dos dados brutos por `TTL` declarativo (30 dias, RN-15). | C |
| RF-38 | O endpoint de histórico deve aceitar janelas maiores (24 h, 7 d, 30 d), lendo os agregados; janelas curtas continuam vindo da memória. | C |
| RF-39 | O ETL deve ser idempotente: reprocessar o mesmo arquivo de spool não pode duplicar dados nas consultas. | S |
| RF-40 | O ETL deve mover arquivos processados para um diretório separado apenas após confirmação do `INSERT`, permitindo reprocessamento em caso de falha. | S |

---

## Resumo de escopo

| Prioridade | Quantidade | Observação |
|---|---|---|
| **Must** | **22** | Define o MVP demonstrável em 05/10 — **todos no caminho quente (.NET + React)** |
| **Should** | **12** | Alvo realista se as semanas 2 e 3 correrem bem |
| **Could** | **6** | Só após todos os *Must* e *Should* fechados |
| **Total** | **40** | RF-01 a RF-40 |

Distribuição dos 22 *Must*: ingestão 4 · cálculo 6 · API 4 · interface 8. Nenhum na seção de persistência — é o que torna a linha de corte de [09 §4](./09-arquitetura-e-stack.md) executável.

**Critério de pronto do MVP:** todos os 22 RF marcados **[M]** implementados, com o painel exibindo dados ao vivo da Mainnet por no mínimo 30 minutos ininterruptos sem intervenção manual — **e sem depender de ClickHouse nem do ETL Python estarem no ar** (RNF-30).
