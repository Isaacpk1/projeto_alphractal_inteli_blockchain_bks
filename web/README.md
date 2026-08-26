# web/ — painel React (o "V" do MVC)

**Dono:** front. **Consome:** SSE e JSON da API em [`api/`](../api/README.md).
**Não fala com o ClickHouse nem com a rede Ethereum** — só com a API.

Andaime criado, painel ainda não implementado. Stack: React 19 + TypeScript strict
+ Vite (RNF-13).

```
src/
├── main.tsx              ponto de entrada
├── App.tsx               composição da tela
├── components/           componentes de apresentação — não buscam dado
├── hooks/                useFeesStream.ts — a assinatura SSE
├── lib/                  api.ts — os endpoints, num lugar só
└── types/
    └── contract.ts       ► espelho de Models/Responses/ da API
```

## Por que esta pasta é a "View"

O ASP.NET não renderiza tela neste projeto: ele devolve JSON e SSE, e o HTML é
montado aqui, no navegador. A View não deixou de existir — mudou de processo.
Ver [`docs/adr/001-mvc-sem-views.md`](../docs/adr/001-mvc-sem-views.md).

E é por causa do tempo real que tinha que ser assim: o painel recebe um bloco novo
a cada ~12 s e atualiza sozinho, com alvo de menos de 2 s entre o bloco e a tela
(RNF-01). Razor entrega HTML uma vez, no request — atualizar exigiria recarregar
a página.

## `types/contract.ts` é a metade frágil do projeto

Ele espelha `Models/Responses/` da API. **Nenhum compilador verifica os dois
lados.** Campo renomeado no C# e não renomeado aqui = tela quebrada em runtime,
sem erro de build em lugar nenhum. Regra: mudou o DTO na API, muda aqui **no mesmo
PR** — é para isso que as duas pastas estão no mesmo repositório.

## Rodar

```bash
cd web
npm install
npm run dev          # http://localhost:5173
```

O Vite faz proxy de `/api` para `http://localhost:5080`, então em dev não há CORS
e o SSE passa sem buffering. Requer a API de [`api/`](../api/README.md) no ar.

```bash
npm run typecheck    # tsc --noEmit, strict
npm run build
```
