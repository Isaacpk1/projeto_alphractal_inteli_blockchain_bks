# ADR-003 — Métricas históricas: as cinco de taxa, traduzidas para Ethereum

- **Status:** aceita · **Data:** 25/08/2026
- **Contexto:** [ADR-002](./002-arquitetura-frontend.md) · [RF-38](../requisitos/01-requisitos-funcionais.md) · [benchmarking §3.2](../analise_negocios_alphractal/04-benchmarking-competitivo.md)

---

## Contexto

A plataforma de referência expõe cinco métricas de taxa, exploradas com BTC
selecionado: **Total Fees**, **Total Fees (USD)**, **Mean Tx Fee**,
**Mean Tx Fee (USD)** e **Mean Tx Fee per Byte** — todas com os mesmos
controles (rate of change, style, scale, smoothing, period, export).

A aba **Historical Fees** deste módulo existia como um gráfico único com três
stat tiles: fina demais para uma aba própria, e sem parentesco com a forma
como o parceiro apresenta métricas.

## Decisão

**Cada métrica é uma rota própria** (`/metrics/:metricId`) e aparece na
navegação lateral, sob a seção *Fee metrics*, ao lado de Real-Time Gas. A aba
agregadora "Historical Fees" deixou de existir: ela era um nível de navegação a
mais para chegar ao mesmo lugar.

O painel **Real-Time Gas não muda** — continua sendo o caminho quente, os 22 RF
*Must* e a demonstração do projeto.

**A métrica vive na rota; a "lente" vive na query string.** Período,
suavização, escalas e estilo seguem no `?`, e a sidebar os carrega ao trocar de
métrica: são a lente do usuário, não parte da métrica. Trocar de métrica com
"1Y + EMA 14d" aplicado mantém "1Y + EMA 14d".

**Não há seletor de rede.** A barra superior identifica *Ethereum · Mainnet* de
forma estática. O módulo é Ethereum, e oferecer um seletor com uma opção só —
ou com opções desabilitadas — promete algo que a §"o que não foi copiado"
abaixo explica que não virá.

### A tradução das métricas

| Métrica de referência (BTC) | Neste módulo (ETH) | Observação |
|---|---|---|
| Total Fees | `total-fees-eth` | direto |
| Total Fees (USD) | `total-fees-usd` | direto |
| Mean Tx Fee | `mean-tx-fee-eth` | direto |
| Mean Tx Fee (USD) | `mean-tx-fee-usd` | direto |
| **Mean Tx Fee per Byte** | **`mean-fee-per-gas` (gwei)** | ⚠️ ver abaixo |

As quatro primeiras dependem do total efetivamente pago, persistido como
`total_fee_wei` a partir dos recibos. Não se recompõe esse valor usando a mediana
da gorjeta; a razão e a validação estão na [ADR-004](./004-total-de-taxas-vem-do-recibo.md).

**A quinta não é tradução literal, e não poderia ser.** Bitcoin cobra por
**byte** de blockspace; Ethereum cobra por **unidade de gas**. "Fee per byte"
não existe no Ethereum — a métrica equivalente, que responde à mesma pergunta
("qual o preço puro do blockspace, sem o viés de transações maiores?"), é a
taxa por unidade de gas, que é literalmente o **gwei**. É o mesmo conceito do
`sat/vB`, na unidade correta da rede.

Vale registrar a consequência: como o gwei já é a unidade nativa da base fee,
essa métrica é a ponte entre as duas abas — é o mesmo número que o painel ao
vivo mostra em tempo real, agregado no tempo.

## O que foi deliberadamente NÃO copiado

**Cobertura de 17 e 37 ativos.** A referência serve essas métricas para dezenas
de ativos. Este módulo é Ethereum, e o seletor de rede foi **removido** da
barra superior — não desabilitado, removido.
O [benchmarking §6](../analise_negocios_alphractal/04-benchmarking-competitivo.md)
é explícito: *"Cobertura de redes é uma armadilha. Owlracle e Dune vencem essa
disputa e ela não gera receita. Duas ou três redes bem instrumentadas valem
mais que quinze rasas."* A ação recomendada na matriz de gaps é **não
perseguir** amplitude.

**Workbench, Add to Dashboard, MCP, API Docs.** São recursos da plataforma
hospedeira, não do módulo. Implementar casca de botão que não faz nada é pior
do que não ter o botão.

**As ações que sobraram foram implementadas de verdade:** Favorite (persistido
em `localStorage`), Share (copia a URL, que reproduz a visão exata), Export CSV
(gerado no cliente a partir da série exibida) e fullscreen.

## Ressalva de escopo — estas métricas não são o produto

O [backlog de diferenciais](../requisitos/05-backlog-diferenciais.md) define o
critério de entrada de qualquer feature: *"um diferencial só entra se responder
à pergunta 'o que eu faço agora?' — não à pergunta 'qual é o número?'.
Mostrar mais um número não agrega."*

As cinco métricas respondem **"qual é o número"**. Elas são contexto histórico
legítimo e alinham o módulo à linguagem visual do parceiro — mas não são
diferencial competitivo, e não substituem o painel ao vivo nem o backlog D-01
a D-12. O quadrante que o benchmarking aponta como vazio continua sendo
ocupado pelo caminho quente, não por esta aba.

## Notas de implementação que custaram a descobrir

**1. Lead-in é obrigatório.** Pedir "YoY sobre 1 ano" com suavização de 30 dias
precisa de 395 dias de série para produzir 365 pontos. Sem lead-in o gráfico
sai vazio para as combinações mais úteis. O contrato ganhou o parâmetro
`lookbackDays` e o campo `from`: a API devolve mais série do que o período, o
cliente transforma e depois recorta em `from`.

**2. Escala log e taxa de variação são incompatíveis.** Variação percentual é
negativa metade do tempo; log exige domínio positivo. O controle de escala da
métrica é desabilitado quando há rate of change, com o motivo no `title`.

**3. A URL é espelho, não fonte da verdade.** A primeira versão lia os
controles direto de `useSearchParams`. Como `setSearchParams` propaga de forma
assíncrona, duas mudanças de controle em sequência rápida liam a mesma URL
antiga e uma apagava a outra — reproduzível em automação, invisível para
humano. A fonte da verdade passou a ser estado local, com a URL escrita como
espelho (sempre `replace`, portanto sem histórico intra-página a reconciliar) e
lida apenas na montagem, para abrir links compartilhados.

## Auditoria de escopo — 26/08/2026

Revisão do painel contra o BRD, os RF/RNF/RN e o backlog. Removido o que os
documentos excluem ou o que exibiria dado que o sistema não pode produzir:

| Removido | Por quê |
|---|---|
| Métrica de **mempool** no cabeçalho | BRD exclui duas vezes — não-objetivo (*"foi exatamente o custo que inviabilizou a Blocknative"*) e tabela de fora de escopo. RF-07 é [C]; [08 — Orçamento RPC](../requisitos/08-orcamento-rpc.md) mostra que a subscription queima a cota mensal em ~7 dias |
| Painel **Gas Guzzlers** | É o D-06 (Onda 2). O backlog proíbe iniciar antes dos 22 RF [M]; a RNF-05 derruba a margem de CU a 27%, abaixo dos 30% exigidos — *"aprovar só com plano pago"* |
| Períodos **90D, MTD, YTD, 1Y, 3Y, 5Y, ALL** | A RF-38 nomeia exatamente 24 h, 7 d e 30 d. A RN-15 retém 30 dias em bruto, o backfill (RF-36) é [C], e o projeto tem 4 semanas de coleta |
| Suavização de **50 e 90 dias**; RoC **QoQ, HoH, YoY** | Sem sentido sobre janela máxima de 30 dias |
| Backbone do mock de **10 anos → 120 dias** | Sintetizar anos dava ao mock profundidade histórica que o sistema real não terá |
| Botão de **suporte** e **avatar de conta** | Sem requisito. O avatar sugeria autenticação num módulo onde a RNF-24 declara que não se coleta dado pessoal |
| **Favoritar**, **compartilhar** e **tela cheia** | Cromo da plataforma de referência, sem requisito em nenhum documento. A exportação CSV ficou porque é o RF-14 do BRD |

**Mantido, com justificativa:**

- **EIP-1559 Burn Rate** — ausente dos RF-01..RF-40, mas é **RF-06 Must do BRD**
  (*"Calcular fee burn acumulado e emissão líquida de ETH"*, também em §4.1).
  Os dois documentos de escopo divergem; nenhum o exclui. Pergunta de kick-off.
- **Overlay de preço do ETH** — é o RF-13 do BRD (*"sobreposição de métrica de
  gas com métrica de outro domínio no mesmo gráfico"*) e satisfaz o OE-06.
- **Export CSV** — RF-14 do BRD, [C].
- **Alerta por limiar** — RF-30 [C] e RF-09 Should do BRD.
- **Tema claro** — a RF-31 pede o tema escuro como identidade, mas o parceiro
  do projeto forneceu mockups dos dois modos.

## Consequências

**Positivas:** a aba passa a ter densidade compatível com uma rota própria; o
módulo fala a língua de métricas do parceiro; o contrato `MetricSeries` já
define o que a API .NET precisa servir; links reproduzem a visão exata.

**Negativas:** mais superfície para o back-end entregar no caminho frio, que é
justamente o candidato a corte se a semana 3 apertar
([09 §4](../requisitos/09-arquitetura-e-stack.md)). Mitigação: a rota inteira é
descartável — nada no caminho quente a referencia.

**Neutras:** as transformações (SMA/EMA e rate of change) rodam hoje no cliente
porque a API não existe. O ClickHouse tem janelas móveis nativas; migrar para o
servidor depois não muda a interface, só para de transformar localmente.
