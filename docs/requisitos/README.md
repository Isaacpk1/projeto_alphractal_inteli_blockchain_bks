# Especificação de Requisitos

**Projeto:** Sistema de monitoramento em tempo real de custos de taxa na rede Ethereum
**Parceiro:** Alphractal (Nortech Labs) · **Executor:** Inteli Blockchain — Diretoria de Projetos
**Período:** 14/09/2026 (kick-off) → 05/10/2026 (Demo Day) · **Entrega:** Protótipo Funcional (MVP)

---

## Contexto

A aba "Fees" da Alphractal hoje se apoia em **médias históricas estáticas**, o que cria um ponto cego frente à volatilidade instantânea da rede. O projeto entrega uma **camada de telemetria ao vivo** que traduz dados brutos da blockchain em custo financeiro imediato, dando previsibilidade a gestores de fundos na execução de operações de alto volume.

O diferencial não é mostrar mais números — é responder **"o que eu faço agora?"**.

## Arquitetura de referência

```
Nó RPC (Alchemy/Infura)
        │  WebSocket (newHeads, eth_feeHistory)
        ▼
Backend Node.js + TypeScript
  ├── provider   → conexão RPC, reconexão, backfill
  ├── service    → cálculo de faixas, USD, congestionamento  (regras de negócio)
  ├── repository → persistência (SQLite → Postgres)
  └── transport  → SSE + REST
        │  SSE (1 conexão RPC → N clientes)
        ▼
Frontend React + Vite + TypeScript  →  painel da aba "Fees"
```

**Restrições do TAP:** somente leitura (nenhuma transação assinada ou enviada), sem deploy em Mainnet, sem integração no ambiente de produção da Alphractal, sem auditoria formal de segurança.

## Índice

| Documento | Conteúdo |
|---|---|
| [01 — Requisitos Funcionais](./01-requisitos-funcionais.md) | RF-01 a RF-38 — o que o sistema faz |
| [02 — Requisitos Não Funcionais](./02-requisitos-nao-funcionais.md) | RNF-01 a RNF-29 — como o sistema se comporta |
| [03 — Regras de Negócio](./03-regras-de-negocio.md) | RN-01 a RN-16 — fórmulas e decisões de domínio |
| [04 — Persistência e Banco de Dados](./04-persistencia-banco-de-dados.md) | Decisão de banco, modelo de dados, retenção |
| [05 — Backlog de Diferenciais](./05-backlog-diferenciais.md) | Ideias de valor agregado, com esforço e critérios de aceite |
| [06 — Dúvidas para o Kick-off](./06-duvidas-kickoff.md) | Perguntas a levar em 14/09 |
| [07 — Riscos](./07-riscos.md) | Riscos técnicos e mitigações |
| [08 — Orçamento RPC](./08-orcamento-rpc.md) | Custo em Compute Units por funcionalidade e limites do plano |

## Convenções

- **RF** = requisito funcional · **RNF** = requisito não funcional · **RN** = regra de negócio · **D** = diferencial (backlog)
- Prioridade **MoSCoW**: **[M]** Must (obrigatório no MVP) · **[S]** Should (importante, sacrificável sob pressão) · **[C]** Could (só se sobrar tempo)
- Itens marcados *(a validar)* dependem de resposta do parceiro — ver [dúvidas](./06-duvidas-kickoff.md).
- IDs são **imutáveis**: requisito removido é marcado como cancelado, nunca reaproveitado.
