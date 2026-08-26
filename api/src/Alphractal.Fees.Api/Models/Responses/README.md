# Models/Responses — o contrato com o front

DTOs que saem da API: payload dos eventos SSE e respostas REST.

**Por que é separado de `Domain/`**

A View deste projeto é o React, em outro repositório e em outra linguagem. Estes
DTOs são a **única amarra** entre os dois lados — mudar um campo aqui quebra a tela
sem gerar erro de compilação em lugar nenhum. É a fonte única do contrato (RNF-13).

**Regras**

- Nome de campo em `camelCase`, estável. Renomear é breaking change: avise o front.
- Unidades já convertidas: gwei, ETH e USD. Wei não sai da API.
- Muda quando **o front pede formato diferente**, não quando a rede muda.
