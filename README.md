# Monitoramento em tempo real de custos de taxa na rede Ethereum

Módulo de telemetria ao vivo para a aba **"Fees"** da plataforma [Alphractal](https://alphractal.com), desenvolvido pela **Diretoria de Projetos do Inteli Blockchain**.

> **Estado:** especificação de requisitos concluída · desenvolvimento inicia em 14/09/2026
> **Entrega:** Protótipo Funcional (MVP) · Demo Day em 05/10/2026

---

## O problema

A aba "Fees" da Alphractal se apoia hoje em **médias históricas estáticas**. Isso cria um ponto cego frente à volatilidade instantânea da rede: gestores de fundos planejam operações de alto volume com estimativas que já nasceram velhas, e o resultado é ordem travada ou custo excessivo em picos não previstos.

Este projeto entrega a camada que falta — **telemetria ao vivo que traduz dados brutos da blockchain em custo financeiro imediato**. O diferencial não é mostrar mais números: é responder *"o que eu faço agora?"*.

## O que o sistema faz

- Acompanha **cada novo bloco** do Ethereum via WebSocket e converte gas em custo real, em ETH e USD
- Estima três faixas de velocidade (**Lento / Padrão / Rápido**) a partir dos percentis de *priority fee*
- Calcula um **índice de congestionamento** da rede e projeta a base fee do próximo bloco pela regra determinística do EIP-1559
- Estima o custo por **tipo de operação** — transferência, ERC-20, swap, approve, mint
- Entrega tudo ao painel por **SSE**, com latência-alvo de **menos de 2 segundos** entre o bloco e a tela

## Arquitetura

Dois caminhos independentes. **O tempo real nunca passa por Python nem por ClickHouse** — essa é a regra estrutural do projeto (RN-14).

```
CAMINHO QUENTE (< 2 s)                       CAMINHO FRIO (minutos)

Alchemy ──WebSocket──▶ .NET (MVC)            .NET ──spool NDJSON──▶ Python ETL
                        ├─ Nethereum                                     │
                        ├─ regras RN-01..05                              ▼
                        ├─ 300 blocos em RAM                        ClickHouse
                        └─ fan-out Channel<T>                     (materialized
                              │ SSE                                   views)
                              ▼                                          │
                        React (painel)  ◀──── /api/history ── .NET ◀──────┘
```

O painel ao vivo precisa ser demonstrável **sem** o caminho frio no ar. Se o cronograma apertar, corta-se ClickHouse e Python inteiros sem comprometer a entrega principal.

## Stack

| Camada | Tecnologia |
|---|---|
| Frontend | React |
| Backend / API | .NET (ASP.NET Core), estrutura MVC |
| Ingestão on-chain | Nethereum via WebSocket |
| ETL | Python |
| Banco analítico | ClickHouse |

Stack definida pela Alphractal — é a infraestrutura de produção deles, o que aumenta a chance de o código ser absorvido ao fim do projeto.

## Documentação

A especificação completa está em **[`docs/requisitos/`](./docs/requisitos/README.md)** — comece pelo índice.

| | |
|---|---|
| [Requisitos funcionais](./docs/requisitos/01-requisitos-funcionais.md) | RF-01 a RF-40 |
| [Requisitos não funcionais](./docs/requisitos/02-requisitos-nao-funcionais.md) | RNF-01 a RNF-31 |
| [Regras de negócio](./docs/requisitos/03-regras-de-negocio.md) | RN-01 a RN-16 — as fórmulas |
| [Arquitetura](./docs/requisitos/09-arquitetura-e-stack.md) | caminho quente vs frio, MVC, linha de corte |
| [Riscos](./docs/requisitos/07-riscos.md) | R-01 a R-20 |
| [Orçamento RPC](./docs/requisitos/08-orcamento-rpc.md) | consumo em Compute Units por funcionalidade |

## Como rodar

> Ainda não implementado — o desenvolvimento começa após o kick-off de 14/09/2026.

A execução local será por `docker-compose up`, com quatro componentes (React, .NET, Python, ClickHouse). Instruções completas entram aqui quando o código existir.

## Escopo — o que este projeto **não** é

Restrições contratuais do TAP, não escolhas técnicas:

- **Somente leitura.** O sistema nunca assina, envia ou simula o envio de transações. Nenhuma chave privada, nenhuma carteira
- **Sem deploy em Mainnet.** Nenhum contrato próprio, nenhum consumo de gas real
- **Sem integração em produção.** A entrega é um protótipo funcional em ambiente isolado
- **Sem auditoria de segurança** formal nem certificação de conformidade financeira

## Licença

[MIT](./LICENSE). Código aberto por decisão do parceiro — os alunos podem usar como portfólio e a Alphractal pode utilizá-lo livremente.

## Aviso

Projeto de caráter **acadêmico e experimental**. O Inteli Blockchain não oferece manutenção, suporte ou correção de bugs após o encerramento do projeto.

---

<sub>Inteli Blockchain — Diretoria de Projetos · em parceria com Alphractal (Nortech Labs) · 2026</sub>
