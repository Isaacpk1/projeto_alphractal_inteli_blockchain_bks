# components/ — apresentação pura

Componentes que recebem dado por prop e devolvem tela. **Não buscam dado**: quem
assina o SSE é `hooks/useFeesStream.ts`, e quem compõe é `App.tsx`.

**Regras**

- Nenhum `fetch` e nenhum `EventSource` aqui dentro.
- Nenhum cálculo de taxa. A API já entrega gwei, ETH e USD prontos — se você está
  dividindo por 1e9 no front, o número está vindo errado da API.
- Formatação de exibição (casas decimais, separador, cor do estado) é daqui.
