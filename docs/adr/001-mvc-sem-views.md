# ADR-001 — Estrutura MVC sem pasta `Views/`

- **Status:** aceita provisoriamente · **a confirmar em ata no kick-off de 14/09/2026**
- **Data:** 26/08/2026
- **Contexto:** dúvidas [22 e 28](../requisitos/06-duvidas-kickoff.md) · [09 §2](../requisitos/09-arquitetura-e-stack.md)

---

## Contexto

Em 18/08/2026 o parceiro definiu a stack por mensagem de WhatsApp e escreveu
*"estrutura MVC"* para o back-end .NET, sem detalhar. A frase é ambígua: no
ASP.NET Core existem **dois templates diferentes** que atendem por esse nome.

| | `dotnet new mvc` | `dotnet new webapi` |
|---|---|---|
| Pastas | `Controllers/`, `Models/`, **`Views/`** | `Controllers/`, `Models/` |
| O controller devolve | `View()` → HTML montado no servidor | `Ok(dado)` → JSON |
| Quem renderiza a tela | Razor, no .NET | o cliente, a partir do JSON |

Escolher errado não é detalhe de organização: muda quem renderiza a interface e,
por consequência, se o React continua existindo no projeto.

## Decisão

**Web API com Controllers. Sem pasta `Views/` e sem Razor.**

A estrutura MVC do projeto é `Controllers/` + `Models/`, com `Models/` dividido em
`Domain/` (entidades e janela quente) e `Responses/` (DTOs do contrato SSE e REST).
A camada de apresentação é o painel React, em [`web/`](../../web/README.md) — projeto
e tecnologia separados, no mesmo repositório.

## Justificativa

**1. SSE torna Razor inviável.** O painel recebe um bloco novo a cada ~12 s e
atualiza sozinho, com alvo de menos de 2 s entre o bloco e a tela (RNF-01). Razor
entrega HTML uma vez, no momento do request — atualizar exigiria recarregar a
página. Um painel ao vivo com Razor puro não atende o requisito.

**2. O TAP já define React como frontend.** Manter Razor e React ao mesmo tempo
significa renderizar a mesma tela por dois caminhos, com duas fontes de verdade
sobre formatação e estado. Isso é duplicação, não arquitetura.

**3. A stack é a de produção da Alphractal.** O front deles é React. Uma tela
Razor não é absorvível — e absorção do código ao fim do projeto é um benefício
declarado no TAP.

**4. Continua sendo o framework MVC, literalmente.** O `ControllerBase` que a API
herda vem de `Microsoft.AspNetCore.Mvc`. Roteamento por atributo, model binding,
validação, filters e injeção de dependência são todos o pipeline MVC. A única
peça do framework que não é usada é o **view engine** (Razor).

## O que isso significa para cada letra

| Letra | Onde vive | Responsabilidade |
|---|---|---|
| **Model** | `Models/Domain/` e `Models/Responses/` | dado e contrato. Não sabe que a web existe |
| **View** | React, em `web/` | exibir. Não calcula nada |
| **Controller** | `Controllers/` | receber, orquestrar, serializar. Não calcula e não desenha |

A View não foi eliminada — **mudou de processo**. Antes rodaria no servidor,
agora roda no navegador. O papel é idêntico.

`Models/Responses/` ganha um peso extra por causa disso: como a View está em outro
processo e em outra linguagem, o DTO de resposta é a **única amarra** entre os
dois lados — espelhado em `web/src/types/contract.ts`, sem compilador que valide
os dois. Mudar um campo ali quebra a tela sem erro de compilação em lugar
nenhum. É por isso que ele é pasta separada de `Models/Domain/` — o de domínio
muda quando a rede muda, o de resposta muda quando o front pede formato diferente.

## Consequências

**Positivas**
- Uma única camada de apresentação, com um único responsável.
- O contrato entre back e front fica explícito em `Models/Responses/`.
- SSE em action de controller retornando `IAsyncEnumerable<T>` funciona sem ginástica.

**Negativas**
- "MVC sem View" é pergunta previsível em banca e exige explicação preparada.
- Sem Razor, não existe página servida pelo .NET: qualquer diagnóstico visual
  depende do front estar no ar. Mitigado pelo `HealthController` em JSON.
- O contrato entre `Models/Responses/` e `web/src/types/contract.ts` não é
  verificado por nenhum compilador. Mudança de campo exige alterar os dois no
  mesmo PR — monorepo torna isso possível, não automático.

**Neutras**
- A pasta `Views/` pode ser adicionada depois sem quebrar nada, caso a decisão
  seja revertida. O custo da reversão está na tela, não na estrutura.

## Alternativas consideradas

**MVC clássico com Razor, sem React.** Descartada: não atende o RNF-01 e contraria
o TAP, que define React como frontend.

**Razor para o casco da página + React embutido como widget.** Descartada: entrega
as desvantagens das duas abordagens — duas camadas de apresentação para manter, e
nenhum ganho, já que o painel é a página inteira.

**`Views/` vazia, só para "cumprir" o nome.** Descartada: pasta decorativa é pior
que pasta ausente. Ela sugere ao próximo dev que existe renderização no servidor.

## Pendência

A frase do parceiro não especificou qual dos dois templates ele quis dizer. A
pergunta precisa ser feita de forma **binária** no kick-off de 14/09 (dúvida 22):

> *"Estrutura MVC = Web API com Controllers, com o React renderizando a tela; ou
> MVC com Razor, com o servidor renderizando? Se for Razor, o React sai do escopo?"*

Se a resposta for Razor, esta ADR é substituída e o impacto é grande: o React sai
ou vira widget, e o back-end passa a renderizar. Descobrir isso na semana 3 custa
o cronograma inteiro.
