# Monitoramento em tempo real de custos de taxa na rede Ethereum

Módulo de telemetria ao vivo para a aba **"Fees"** da plataforma [Alphractal](https://alphractal.com), desenvolvido pela **Diretoria de Projetos do Inteli Blockchain**.

> **Estado:** protótipo funcional integrado · caminho quente e frio validados localmente em 01/09/2026
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
- Soma a taxa **efetivamente paga** em cada bloco pelos recibos (`gasUsed × effectiveGasPrice`) e publica os agregados históricos em ETH e USD
- Entrega tudo ao painel por **SSE**, com latência-alvo de **menos de 2 segundos** entre o bloco e a tela

## Demonstração

O vídeo mostra o sistema rodando de ponta a ponta: o painel recebendo bloco a
bloco pelo SSE, as faixas de velocidade e o índice de congestionamento se
movendo com a rede, e a aba de métricas históricas servida pelo ClickHouse.

**▶ [Assistir à demonstração](https://drive.google.com/file/d/1U2_OOmxOk1aow6j7YpKVERHS2CP1kAMU/view?usp=sharing)** · Google Drive

Vale assistir antes de subir o ambiente: o caminho quente depende de uma chave
da Alchemy, e o vídeo mostra o comportamento ao vivo sem precisar de uma.

## Arquitetura

Dois caminhos independentes. **O tempo real nunca passa por Python nem por ClickHouse** — essa é a regra estrutural do projeto (RN-14).

```
CAMINHO QUENTE (< 2 s)                       CAMINHO FRIO (minutos)

Alchemy ──WS + HTTP──▶ .NET (MVC)            .NET ──spool NDJSON──▶ Python ETL
                        ├─ newHeads + recibos                            │
                        ├─ regras RN-01..05                              ▼
                        ├─ 300 blocos em RAM                        ClickHouse
                        └─ broadcaster SSE                       (rollups
                              │                                    idempotentes)
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

## Estrutura do repositório

Quatro pastas de código, uma de documentação. Um dono por pasta — PR que mexe em
pasta de outro dono precisa do aval dele.

| Pasta | Dono | O que é |
|---|---|---|
| [`api/`](./api/README.md) | back-end | API .NET 10, estrutura MVC. Ingestão via Nethereum, cálculo das regras, SSE para o painel. É o caminho quente inteiro. |
| [`etl/`](./etl/README.md) | ingestão | Python. Lê o spool NDJSON da API, trata e carrega no ClickHouse. Caminho frio. |
| [`infra/`](./infra/README.md) | infra | ClickHouse em Docker + schema em três camadas. Única fonte do schema — nenhum `CREATE TABLE` mora fora daqui. |
| [`web/`](./web/README.md) | front | Painel React 19 + TypeScript strict + Vite. Consome SSE e JSON da API. É o "V" do MVC. |
| [`docs/`](./docs/requisitos/README.md) | todos | Requisitos, regras de negócio, arquitetura e análise de negócios. Muda no mesmo commit do código. |

`api/` e `web/` ficam no mesmo repositório de propósito: `Models/Responses/` e
`web/src/types/contract.ts` são os dois lados do mesmo contrato, e nenhum
compilador verifica se eles batem. Mudou um, muda o outro **no mesmo PR**.

## Documentação

A especificação completa está em **[`docs/requisitos/`](./docs/requisitos/README.md)** — comece pelo índice.

| | |
|---|---|
| [Requisitos funcionais](./docs/requisitos/01-requisitos-funcionais.md) | RF-01 a RF-40 |
| [Requisitos não funcionais](./docs/requisitos/02-requisitos-nao-funcionais.md) | RNF-01 a RNF-31 |
| [Regras de negócio](./docs/requisitos/03-regras-de-negocio.md) | RN-01 a RN-17 — as fórmulas |
| [Arquitetura](./docs/requisitos/09-arquitetura-e-stack.md) | caminho quente vs frio, MVC, linha de corte |
| [Riscos](./docs/requisitos/07-riscos.md) | R-01 a R-20 |
| [ADR-001](./docs/adr/001-mvc-sem-views.md) | por que não existe pasta `Views/` |
| [ADR-002](./docs/adr/002-arquitetura-frontend.md) | arquitetura e isolamento de renderização do frontend |
| [ADR-003](./docs/adr/003-metricas-historicas.md) | catálogo e navegação das métricas históricas |
| [ADR-004](./docs/adr/004-total-de-taxas-vem-do-recibo.md) | por que Total Fees vem dos recibos, não da mediana da gorjeta |
| [Orçamento RPC](./docs/requisitos/08-orcamento-rpc.md) | consumo em Compute Units por funcionalidade |

## Como rodar

O ambiente completo de dados e API sobe pelo Compose; o Vite roda no host:

```powershell
cd infra
docker compose up -d --build

cd ..\web
npm install
npm run dev
```

Abra `http://localhost:5173`. A API fica em `http://localhost:5080`; o Vite
encaminha `/api` para ela. Consulte os guias de [`infra/`](./infra/README.md),
[`api/`](./api/README.md), [`etl/`](./etl/README.md) e [`web/`](./web/README.md).

Copie os arquivos `.env.example` e use chaves locais. Arquivos `.env` são
ignorados pelo Git; se uma chave aparecer em log, captura de tela ou conversa,
revogue-a e gere outra.

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
