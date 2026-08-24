[← Índice](./README.md)

# 10 — Registro de Respostas do Parceiro

> **Fonte:** conversa por WhatsApp com **Kadota Manauara** (Alphractal / Nortech Labs) em **18/08/2026**, entre 18:54 e 19:13.
> Este documento é a **procedência** das decisões tomadas fora do TAP. Sempre que a especificação disser "definido pelo parceiro", a origem está aqui.
> Respostas obtidas **antes** do kick-off oficial (14/09/2026) — o que não as invalida, mas significa que devem ser **reconfirmadas em ata** na reunião de abertura.

---

## 1. Transcrição por tema

| Tema | Pergunta levada | Resposta do parceiro (verbatim) |
|---|---|---|
| Métricas | Quais métricas exibir? | *"métricas livres, leves e soltas"* · *"4 métricas misteriosas"* |
| Armazenamento | Dados dentro do sistema ou não? Qual database, ou on-chain? | *"tem várias formas, escolhe aê"* · *"salva no banco deles, ClickHouse"* |
| Frequência | Qual a frequência ideal de atualização? | *"nível diário, time frame indeterminado, tão aí pra ajudar a gente a definir"* |
| Provedor / nó | Já tem algum provedor que comunica com um nó? | *"tem um pipeline, faz um trabalho nisso, Doge coin, e tratar os dados né, aí alimenta com uma API pro front end"* |
| Análise de negócio | O que vocês querem de análise de negócios? | *"queremos, pode fazer aê"* |
| Indicadores da aba Fees | Algum indicador específico obrigatório? | *"faz aê, sejam livres, depois a gente manda alguma coisa"* |
| Stack | — | *"Python · React front · C#"* · *"estrutura MVC"* |
| Contexto | — | *"tão procurando pra expandir o time, adaptado ao mercado, ser startup e inovar todo dia"* |

**Stack final consolidada na conversa:**

| Camada | Tecnologia |
|---|---|
| Frontend | React |
| Backend / API | .NET (C#), **estrutura MVC** |
| Tratamento de dados | Python (ETL) |
| Banco | ClickHouse |

---

## 2. Impacto de cada resposta na especificação

| # | Resposta | Dúvida encerrada | Impacto |
|---|---|---|---|
| A | Stack React / .NET / Python / ClickHouse | — | Reescrita dos docs [04](./04-persistencia-banco-de-dados.md) e [09](./09-arquitetura-e-stack.md) (commit de 18/08 19:27) |
| B | **Estrutura MVC** no .NET | 14, 22 (parcial) | [09 §2](./09-arquitetura-e-stack.md) — controllers MVC no lugar de minimal APIs; mapeamento das camadas da RNF-14 sobre MVC |
| C | Banco: ClickHouse, *"salva no banco deles"* | 5 | [04](./04-persistencia-banco-de-dados.md) confirmado. **Ressalva:** gravar na instância de produção deles conflita com a restrição do TAP — ver dúvida 21 |
| D | Métricas e indicadores **livres** | 6, 7, 8 | RN-02, RN-04 e RN-11 deixam de ser *(a validar)* e viram decisão do time. Mantidas em **configuração**, não em código, porque *"depois a gente manda alguma coisa"* |
| E | Querem **análise de negócio** | 10 (parcial) | Valida a direção do backlog [05](./05-backlog-diferenciais.md) — D-01 (custo em bps), D-02 (percentil) e D-04 (heatmap) deixam de ser especulativos |
| F | Pipeline próprio: ingestão → tratamento → API → front | 24, 25 | Confirma a divisão do [09 §1](./09-arquitetura-e-stack.md). **Deixa de ser proposta nossa: é o padrão interno deles** |
| G | Frequência *"nível diário"* | — | Ver §3 abaixo — **não encerra a dúvida, cria uma** |
| H | *"4 métricas misteriosas"* | — | Ver §3 abaixo |
| I | Ponto focal: Kadota Manauara, via WhatsApp | 17 | [06](./06-duvidas-kickoff.md) atualizado |

---

## 3. Duas respostas que precisam de reconfirmação

### 3.1 *"Nível diário"* × RNF-01 (2 segundos) ⚠️

A resposta sobre frequência de atualização — *"nível diário, time frame indeterminado"* — **contradiz frontalmente a premissa do TAP**, que pede *"monitoramento em tempo real"* e *"telemetria e previsibilidade ao vivo"*.

**Interpretação adotada** (a confirmar no kick-off — dúvida 27):

| Camada | Frequência | Origem |
|---|---|---|
| Painel ao vivo (caminho quente) | **< 2 s**, a cada bloco | TAP — é a razão de ser do projeto |
| Histórico e agregados (caminho frio) | **horário e diário** | Resposta do parceiro |

O TAP é o documento contratual e define tempo real como o problema a resolver; uma resposta em conversa informal não o revoga. A leitura mais provável é que *"nível diário"* se refere à **granularidade do dado armazenado e analisado**, não à atualização do painel — o que é compatível, e aliás foi por isso que o rollup diário entrou no [04 §3.2](./04-persistencia-banco-de-dados.md).

**Se a interpretação estiver errada, o escopo do projeto muda por completo.** É a primeira pergunta da pauta de 14/09.

### 3.2 *"4 métricas misteriosas"* ⚠️

Não há informação suficiente para interpretar. As duas leituras possíveis têm consequências opostas:

| Leitura | Consequência |
|---|---|
| A Alphractal tem **4 métricas proprietárias** ainda não reveladas | Podem ser o núcleo do diferencial do painel — muda RF-22 a RF-27 e possivelmente o modelo de dados |
| Querem **4 métricas em destaque** na tela, à nossa escolha | Nenhum impacto — já coberto por *"sejam livres"* |

Registrado como **dúvida 26**. Não deve ser adivinhado: se forem métricas proprietárias, construir o painel sem elas é retrabalho garantido.

---

## 4. O que continua sem resposta

As duas pendências **bloqueantes** não foram tocadas nesta conversa:

- **Dúvida 1 — chave RPC de Ethereum.** A resposta sobre "provedor que comunica com um nó" descreveu o pipeline de **Dogecoin** deles. É referência arquitetural valiosa, mas não é uma conta Alchemy/Infura de Ethereum. Sem ela, a semana 2 começa no plano gratuito, com ~73% de consumo estimado ([08](./08-orcamento-rpc.md)).
- **Dúvida 11 — design e identidade visual.** *"Depois a gente manda alguma coisa"* não destrava o protótipo de alta fidelidade previsto para a semana 1.

---

## 5. Observação de processo

Esta conversa aconteceu **fora das duas reuniões previstas no TAP** e resolveu sete dúvidas. Vale registrar que o canal assíncrono com o parceiro está funcionando melhor do que o TAP previa (48 h úteis) — o que reduz o risco **R-07**, mas **não** substitui a ata do kick-off. Toda decisão desta página deve ser lida em voz alta em 14/09 e confirmada, porque respostas de WhatsApp em fluxo rápido são exatamente o tipo de acordo que ninguém lembra ter feito quando a entrega diverge.
