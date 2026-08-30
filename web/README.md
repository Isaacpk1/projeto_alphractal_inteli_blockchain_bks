# web/ — painel React (o "V" do MVC)

**Dono:** front. **Consome:** SSE e JSON da API em [`api/`](../api/README.md).
**Não fala com o ClickHouse nem com a rede Ethereum** — só com a API.

Stack: React 19 + TypeScript strict + Vite (RNF-13), `react-router-dom` (duas
rotas) e `recharts` (o gráfico do RF-24). Arquitetura registrada na
[ADR-002](../docs/adr/002-arquitetura-frontend.md).

```
src/
├── main.tsx                 router + provider de preferências
├── App.tsx                  o shell: sidebar, topbar, rota ativa. Zero dado de bloco
├── views/                   composição de rota — montam a tela, não formatam nada
│   ├── RealTimeGasView.tsx      caminho quente (RF-22 a RF-33)
│   └── MetricView.tsx           explorador de métrica, caminho frio (ADR-003)
├── components/
│   ├── layout/              Sidebar, TopBar
│   ├── live/                MetricsHeader, ConnectionBar, BlockAge, BaseFeeChart
│   ├── fees/                FeeTierCard, CongestionBadge, TxCostEstimator, AlertBanner
│   ├── metrics/             MetricControls, MetricChart — as rotas de métrica
│   ├── insights/            GasGuzzlers, BurnRate — D-06 (backlog), mock-only
│   └── ui/                  Card, Skeleton, ErrorState, SegmentedControl, ícones
├── hooks/
│   ├── useFeesStream.ts     liga o store (só o App chama; não devolve estado)
│   ├── useFeesSlice.ts      assinatura de FATIA do store — o mecanismo do RNF-03
│   ├── usePreferences.tsx   tema, unidade, tipo de tx, limiar — estado de clique
│   └── useTicker.ts         relógio do "2s ago" — vive na folha, de propósito
├── lib/
│   ├── api.ts               os endpoints, num lugar só
│   ├── feesStore.ts         ► dono do stream e da janela de 300 blocos, FORA do React
│   ├── transport.ts         interface FeesTransport + implementação HTTP real
│   ├── mock/                a "API de mentira" enquanto a de verdade não existe
│   ├── metrics.ts           catálogo das 5 métricas históricas
│   ├── series.ts            SMA/EMA, taxa de variação e CSV
│   └── format.ts            formatação de exibição (a única matemática permitida aqui)
└── types/
    └── contract.ts          ► espelho de Models/Responses/ da API
```

## As rotas

| Rota | O que é |
|---|---|
| `/` | **Real-Time Gas** — caminho quente: SSE, janela de 300 blocos, os 22 RF *Must*. É o projeto. |
| `/metrics/:metricId` | Uma das **cinco métricas** agregadas de taxa — caminho frio |

As cinco métricas aparecem na navegação lateral sob *Fee metrics*:
`total-fees-eth`, `total-fees-usd`, `mean-tx-fee-eth`, `mean-tx-fee-usd` e
`mean-fee-per-gas`. Cada uma tem rate of change, smoothing (SMA/EMA), escalas
independentes para métrica e preço, overlay do preço do ETH e export CSV.

Os períodos são **24H, 7D e 30D** — exatamente os que a RF-38 nomeia. Não há
janela mais longa porque não haveria dado: a RN-15 retém blocos brutos por 30
dias e o projeto tem quatro semanas de coleta. Pelo mesmo motivo, a suavização
vai até 30 dias e a taxa de variação até MoM.

Se o ClickHouse cair, essas rotas falham sozinhas e o painel ao vivo não fica
sabendo (RNF-30). Por que estas cinco, o que foi deliberadamente deixado de
fora e por que "fee per byte" virou "fee per gas":
[ADR-003](../docs/adr/003-metricas-historicas.md).

**A métrica está na rota; a "lente" está na query string.** Período, suavização
e escalas seguem no `?`, então trocar de métrica na sidebar preserva a lente
escolhida, e o link da barra de endereço reproduz a visão exata.

## Rede: só Ethereum

Não há seletor de rede. A barra superior identifica *Ethereum · Mainnet* de
forma estática, porque a escolha não existe — o
[benchmarking §6](../docs/analise_negocios_alphractal/04-benchmarking-competitivo.md)
recomenda **não** perseguir amplitude de redes, e a ADR-003 registra a decisão.

## Por que o estado do stream não mora no React

Um bloco chega a cada ~12 s. Se ele morasse num `useState` do App, a árvore
inteira reconciliaria a cada bloco — o que o RNF-03 proíbe. O `feesStore` vive
fora da árvore; componentes assinam **fatias** via `useFeesSlice`
(`useSyncExternalStore`). Quem lê a base fee re-renderiza por bloco; a sidebar
nunca. Verificação: React DevTools Profiler, como o RNF-03 manda.

Detalhes e alternativas descartadas na [ADR-002](../docs/adr/002-arquitetura-frontend.md).

## API real e modo mock

Por padrão o front usa `HttpFeesTransport` e consome snapshot, SSE, histórico,
cotação e queima da API .NET. Para uma demonstração offline, há uma simulação
da Mainnet em [src/lib/mock/](src/lib/mock/) com a regra do EIP-1559 e um bloco
a cada 12 s. A troca é feita por variável de ambiente:

```bash
# .env
VITE_USE_MOCK=true   # ativa o mock; ausente ou false usa a API real
```

Para testar os estados do RF-26/RF-32 sem esperar a rede cair, no console do
navegador (só em dev):

```js
__afMock.outage(15) // queda de conexão por 15 s → RECONNECTING
__afMock.stale(60)  // conexão ok, sem bloco novo → STALE DATA
```

## `types/contract.ts` é a metade frágil do projeto

Ele espelha `Models/Responses/` da API. **Nenhum compilador verifica os dois
lados.** Campo renomeado no C# e não renomeado aqui = tela quebrada em runtime,
sem erro de build em lugar nenhum. Regra: mudou o DTO na API, muda aqui **no
mesmo PR** — é para isso que as duas pastas estão no mesmo repositório.

Os DTOs reais ficam separados dos modelos de tela em `contract.ts`; o adaptador
em `lib/transport.ts` traduz velocidades, estimativas, histórico e cotação sem
espalhar detalhes do backend pelos componentes.

## Rodar

```bash
cd web
npm install
npm run dev          # http://localhost:5173 — usa a API real
```

Com `VITE_USE_MOCK=false` (padrão), o Vite faz proxy de `/api` para
`http://localhost:5080` (sem CORS, SSE sem buffering) e requer a API de
[`api/`](../api/README.md) no ar.

```bash
npm run typecheck    # tsc --noEmit, strict
npm run build
```
