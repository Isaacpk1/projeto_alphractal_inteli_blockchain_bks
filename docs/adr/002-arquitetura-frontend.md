# ADR-002 — Arquitetura do frontend: store fora do React e transporte trocável

- **Status:** aceita · **Data:** 25/08/2026
- **Contexto:** [ADR-001](./001-mvc-sem-views.md) · [RNF-03, RNF-30](../requisitos/02-requisitos-nao-funcionais.md) · [RF-22 a RF-33](../requisitos/01-requisitos-funcionais.md)

---

## Contexto

O painel é a View do MVC (ADR-001) e recebe um bloco a cada ~12 s pelo SSE. Dois
requisitos moldam a estrutura interna:

- **RNF-03:** o frontend não pode re-renderizar a árvore inteira a cada
  atualização — "nenhum render desnecessário acima do card", verificado com o
  React DevTools Profiler.
- **RNF-30:** o caminho quente não depende de ClickHouse/Python — o painel ao
  vivo funciona com o caminho frio inteiro fora do ar.

Além disso, a API .NET ainda não existe: o front precisa ser demonstrável e
desenvolvível antes dela.

## Decisão

**1. O estado do stream vive fora da árvore React**, num store em módulo
(`lib/feesStore.ts`) que é o dono único do `EventSource` e da janela de 300
blocos. Componentes assinam **fatias** via `useSyncExternalStore`
(`hooks/useFeesSlice.ts`). O App liga o stream mas não lê nada dele.

**2. Três tipos de estado, três lugares:**

| Estado | Muda quando | Onde vive |
|---|---|---|
| Stream (bloco, faixas, status) | a cada ~12 s, sozinho | `feesStore`, fora do React |
| Preferência (tema, unidade, limiar) | o usuário clica | Context (`usePreferences`) |
| Servidor sob demanda (histórico frio) | a view pede | `fetch` local da view |

**3. O acesso à rede passa por uma interface** (`FeesTransport`) com duas
implementações: `HttpFeesTransport` (a API real) e `MockFeesTransport`
(simulação da Mainnet com a regra do EIP-1559). A troca é a env
`VITE_USE_MOCK` — o store não sabe qual está usando.

**4. Relógios ficam nas folhas.** O "há 4s" (RF-25) tica num componente folha
com `setInterval` próprio (`useTicker`); o watchdog de dado atrasado fica no
store e só notifica quando o **status muda**. Nenhum timer acorda a árvore.

**5. Dependências mínimas:** `react-router-dom` (duas rotas do design) e
`recharts` (gráfico do RF-24). Sem Redux/Zustand (o `useSyncExternalStore`
cobre), sem TanStack Query (um endpoint), sem kit de componentes (design
autoral). CSS com custom properties num tema claro/escuro por `data-theme`.

## Justificativa

**RNF-03 vira estrutura, não disciplina.** Com o estado de alta frequência fora
da árvore, é impossível um bloco novo re-renderizar a sidebar: ela não assina
nada. A alternativa (estado no App + `React.memo` em tudo) depende de cada dev
lembrar do memo em cada componente novo — quebra silenciosamente.

**RNF-30 vira isolamento físico.** A rota Historical Fees busca o próprio dado;
se o caminho frio cair, ela falha sozinha e o painel ao vivo não fica sabendo.
A linha de corte de [09 §4](../requisitos/09-arquitetura-e-stack.md) — "cortar o
caminho frio inteiro" — é deletar uma rota.

**O mock destrava o cronograma.** Front e back andam em paralelo: o contrato
(`types/contract.ts`) já é a proposta de `Models/Responses/`, e a troca
mock→real não toca em componente nenhum.

## Armadilhas documentadas (para não redescobrir)

- Seletor de `useFeesSlice` deve devolver **referência estável** (primitivo ou
  objeto que o store recria só quando muda). Objeto novo a cada chamada =
  re-render sempre, no limite loop infinito.
- O timer do "há 4s" na folha, nunca no store — senão todo assinante acorda a
  cada segundo.
- Ao reconectar o SSE, os blocos do intervalo se perderam: o store re-hidrata
  (`snapshot` + `history`) para tapar o degrau do gráfico.
- `EventSource` aberto ≠ dado fresco: o estado "Dados desatualizados" (RF-26)
  vem de um watchdog sobre a idade do último bloco, não do transporte.

## Consequências

**Positivas:** RNF-03 garantido por construção; caminho frio descartável
(RNF-30); front demonstrável sem API; contrato já proposto para o back.

**Negativas:** `useSyncExternalStore` é menos conhecido que `useState` — o time
precisa ler `hooks/useFeesSlice.ts` antes de criar componente que consome
stream. O contrato mockado pode divergir do que o back decidir — mitigado por
`contract.ts` ser proposta explícita, a alinhar antes do primeiro DTO em C#.

**Neutras:** se o painel crescer muito, o store pode virar Zustand (mesma
mecânica por baixo) sem mudar os componentes — a assinatura de `useFeesSlice`
se mantém.
