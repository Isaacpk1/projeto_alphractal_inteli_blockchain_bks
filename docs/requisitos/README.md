# Especificação de Requisitos

**Projeto:** Sistema de monitoramento em tempo real de custos de taxa na rede Ethereum
**Parceiro:** Alphractal (Nortech Labs) · **Executor:** Inteli Blockchain — Diretoria de Projetos
**Período:** 14/09/2026 (kick-off) → 05/10/2026 (Demo Day) · **Entrega:** Protótipo Funcional (MVP)

---

## Contexto

A aba "Fees" da Alphractal hoje se apoia em **médias históricas estáticas**, o que cria um ponto cego frente à volatilidade instantânea da rede. O projeto entrega uma **camada de telemetria ao vivo** que traduz dados brutos da blockchain em custo financeiro imediato, dando previsibilidade a gestores de fundos na execução de operações de alto volume.

O diferencial não é mostrar mais números — é responder **"o que eu faço agora?"**.

## Stack

Definida pela **Alphractal** em 18/08/2026 ([registro](./10-registro-respostas-parceiro.md)) — é a infraestrutura de produção deles, o que aumenta a chance de o código ser absorvido ao fim do projeto.

| Camada | Tecnologia |
|---|---|
| Frontend | React |
| Backend / API | .NET (ASP.NET Core), **estrutura MVC** |
| ETL | Python |
| Banco analítico | ClickHouse (Docker local no protótipo) |

## Arquitetura de referência

Dois caminhos independentes — detalhamento em [09 — Arquitetura e Stack](./09-arquitetura-e-stack.md).

```
CAMINHO QUENTE (< 2 s)                       CAMINHO FRIO (minutos)

Alchemy ──WebSocket──▶ .NET                  .NET ──spool NDJSON──▶ Python ETL
                        ├─ Nethereum                                     │
                        ├─ regras RN-01..05                              ▼
                        ├─ 300 blocos em RAM                        ClickHouse
                        └─ fan-out Channel<T>                     (materialized
                              │ SSE                                   views)
                              ▼                                          │
                        React (painel)  ◀──── /api/history ── .NET ◀──────┘
```

**Regra estrutural:** o tempo real nunca passa por Python nem por ClickHouse (RN-14).

**Restrições do TAP:** somente leitura (nenhuma transação assinada ou enviada), sem deploy em Mainnet, sem integração no ambiente de produção da Alphractal, sem auditoria formal de segurança.

**As três janelas do sistema** (RN-10), todas configuráveis: `N_fee` = 20 blocos (percentis de *priority fee*) · `N_cong` = 100 blocos (média móvel de congestionamento) · `N_buffer` = 300 blocos (gráfico e memória).

## Índice

| Documento | Conteúdo |
|---|---|
| [01 — Requisitos Funcionais](./01-requisitos-funcionais.md) | RF-01 a RF-40 — o que o sistema faz (22 Must · 12 Should · 6 Could) |
| [02 — Requisitos Não Funcionais](./02-requisitos-nao-funcionais.md) | RNF-01 a RNF-31 — como o sistema se comporta |
| [03 — Regras de Negócio](./03-regras-de-negocio.md) | RN-01 a RN-16 — fórmulas e decisões de domínio |
| [04 — Persistência e Banco de Dados](./04-persistencia-banco-de-dados.md) | Decisão de banco, modelo de dados, retenção |
| [05 — Backlog de Diferenciais](./05-backlog-diferenciais.md) | Ideias de valor agregado, com esforço e critérios de aceite |
| [06 — Dúvidas para o Kick-off](./06-duvidas-kickoff.md) | 29 dúvidas — 8 encerradas, 4 bloqueantes abertas |
| [07 — Riscos](./07-riscos.md) | R-01 a R-20 — riscos técnicos e mitigações |
| [08 — Orçamento RPC](./08-orcamento-rpc.md) | Custo em Compute Units por funcionalidade e limites do plano |
| [09 — Arquitetura e Stack](./09-arquitetura-e-stack.md) | Caminho quente vs frio, estrutura MVC, responsabilidades .NET/Python, linha de corte do MVP |
| [10 — Registro de Respostas do Parceiro](./10-registro-respostas-parceiro.md) | Procedência de cada decisão tomada fora do TAP |

## Convenções

- **RF** = requisito funcional · **RNF** = requisito não funcional · **RN** = regra de negócio · **D** = diferencial (backlog)
- Prioridade **MoSCoW**: **[M]** Must (obrigatório no MVP) · **[S]** Should (importante, sacrificável sob pressão) · **[C]** Could (só se sobrar tempo)
- Itens marcados *(a validar)* dependem de resposta do parceiro — ver [dúvidas](./06-duvidas-kickoff.md).
- IDs são **imutáveis**: requisito removido é marcado como ~~cancelado~~, nunca reaproveitado.
- Decisões que **não** vêm do TAP têm procedência registrada no [doc 10](./10-registro-respostas-parceiro.md), com data e citação.

## Estado da especificação

| | |
|---|---|
| Última revisão | 24/08/2026 — incorporação das respostas do parceiro de 18/08 |
| Dúvidas bloqueantes | **4** — chave RPC (1), design (11), "4 métricas misteriosas" (26), "nível diário" (27) |
| Próximo marco | Kick-off, 14/09/2026 |

> ⚠️ **Duas premissas ainda não confirmadas** sustentam parte desta especificação: que *"nível diário"* se refere à granularidade de análise e não à atualização do painel (dúvida 27), e que as *"4 métricas misteriosas"* não são métricas proprietárias obrigatórias (dúvida 26). Ambas estão marcadas em [10 §3](./10-registro-respostas-parceiro.md) e devem abrir a pauta de 14/09.
