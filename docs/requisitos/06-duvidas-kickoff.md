[← Índice](./README.md)

# 06 — Dúvidas para o Kick-off (14/09/2026)

> O TAP prevê apenas **2 reuniões obrigatórias** com o parceiro. Tudo que não for resolvido em 14/09 vira mensagem com SLA de 48 h úteis — o que pode custar dias de trabalho.
> **Numeração canônica.** Esta é a lista mestra; qualquer referência a "dúvida nº X" em outro documento aponta para cá. IDs são imutáveis — dúvida respondida é marcada como encerrada, nunca reaproveitada.

---

## Estado atual

| Estado | Qtd. | Quais |
|---|---|---|
| ✅ Encerradas | 8 | 5, 6, 7, 8, 10 (parcial), 17, 24, 25 |
| 🟡 Parciais | 2 | 14, 22 |
| 🔴 **Bloqueantes abertas** | 4 | **1, 11, 26, 27** |
| ⚪ Abertas | 15 | as demais |

As encerradas vieram da conversa de **18/08/2026** com Kadota Manauara — transcrição e procedência em [10 — Registro de Respostas do Parceiro](./10-registro-respostas-parceiro.md).

---

## 🔴 Bloqueantes

**1. Provedor RPC e chave de API.** Qual provedor usaremos e **quem fornece a chave** — a Alphractal disponibiliza uma conta Alchemy/Infura ou trabalhamos no plano gratuito? Qual o limite de *compute units* aceitável?
→ *Estado:* **aberta.** Em 18/08 perguntamos se já havia provedor comunicando com um nó; a resposta descreveu o pipeline de **Dogecoin** deles — é referência arquitetural, não uma conta de Ethereum.
→ *Por que trava:* sem chave paga, D-06 e multi-rede (D-05) ficam fora, e o ingestor não pode rodar 24/7.
→ *Impacta:* RF-01, RNF-05, D-05, D-06.

**11. Design e identidade visual.** Existe Figma, design system ou tokens de cor da Alphractal que possamos usar para o painel se integrar à aba "Fees"? Podemos receber prints da aba atual?
→ *Estado:* **aberta.** *"Depois a gente manda alguma coisa"* não destrava a entrega da semana 1.
→ *Impacta:* protótipo de alta fidelidade (semana 1), RF-31.

**26. "4 métricas misteriosas".** Em 18/08 vocês mencionaram *"4 métricas misteriosas"*. São métricas proprietárias da Alphractal que devem aparecer no painel, ou eram quatro indicadores em destaque à nossa escolha?
→ *Por que trava:* se forem proprietárias, podem ser o núcleo do diferencial do painel — e construir RF-22 a RF-27 sem elas é retrabalho garantido.
→ *Impacta:* RF-22 a RF-27, possivelmente o modelo de dados.

**27. "Nível diário" — análise ou atualização?** Perguntados sobre frequência ideal de atualização, vocês responderam *"nível diário, time frame indeterminado"*. O TAP pede **monitoramento em tempo real**. Nossa leitura: *diário* é a granularidade do dado **analisado e armazenado**, enquanto o **painel atualiza a cada bloco (~12 s)**. Confere?
→ *Por que trava:* é a diferença entre o projeto do TAP e um projeto completamente diferente.
→ *Estado atual da spec:* implementamos os dois — RNF-01 (< 2 s) no painel, rollup diário no ClickHouse ([04 §3.3](./04-persistencia-banco-de-dados.md)).
→ *Impacta:* RNF-01, R-18, o escopo inteiro.

---

## Dados e infraestrutura

**2. Escopo de redes.** Somente Ethereum Mainnet, ou o painel já deve ser preparado para L2s (Base, Arbitrum, Optimism)? Desenvolvemos contra Sepolia ou direto na Mainnet — leitura é gratuita?

**3. Mempool real ou métricas derivadas?** Vocês querem dados reais de transações pendentes, ou basta `baseFee` + `eth_feeHistory`?
→ *Contexto a levar à reunião (números verificados — ver [08 — Orçamento RPC](./08-orcamento-rpc.md)):* a subscription de transações pendentes **não é bloqueada por plano** na Alchemy, mas é cobrada **por byte entregue** (0,04 CU/byte). Na prática: só com hashes, esgota os 30 M CU do plano gratuito em **~7 dias**; com objetos completos, em **menos de 24 h** — e ainda esbarra no teto de 500 CUPS.
→ *Portanto a pergunta real é:* **vocês têm plano pago?** Em PAYG (US$ 0,45/M CU) a mempool passa a ser uma decisão de orçamento, não de viabilidade. *Impacta:* RF-07.

**4. Fonte de preço ETH/USD.** Já existe internamente um serviço de preço na plataforma que devemos consumir, ou escolhemos a fonte (CoinGecko, Binance, Chainlink)?

**~~5. Histórico e banco de dados.~~** ✅ **Encerrada em 18/08:** ClickHouse, *"salva no banco deles"*.
→ *Desdobramento aberto:* ver dúvida **21** (instância de produção vs Docker local).

---

## Produto e regras

**~~6. Definição de "saúde da rede".~~** ✅ **Encerrada em 18/08:** *"faz aê, sejam livres"*. Definimos nós — RN-04, com faixas em configuração.
→ *A levar como informe:* a RN-04 mede **variação** (base fee contra média móvel de 100 blocos) e o D-02 mede **nível** (percentil histórico). São complementares. Queremos 5 minutos no kick-off para mostrar a leitura em duas dimensões e ouvir se faz sentido para o usuário de vocês.

**~~7. Tipos de transação relevantes.~~** ✅ **Encerrada em 18/08:** liberdade concedida. Definidos em RN-11 (transferência ETH, ERC-20, approve, swap, mint), em arquivo de configuração.
→ *Informe:* se vocês tiverem os tipos que o usuário institucional mais executa, trocamos os valores sem tocar em código.

**~~8. Padrão das faixas de velocidade.~~** ✅ **Encerrada em 18/08:** liberdade concedida. RN-02 usa p25/p50/p90 sobre 20 blocos.

**9. Alertas.** O painel deve incluir alertas/notificações — visual apenas, ou também e-mail/Telegram? Está no MVP ou é evolução futura?
→ *Impacta:* RF-30, D-12.

**10. Métrica de sucesso.** ✅ **Parcial:** em 18/08 confirmaram que querem **análise de negócio** (*"queremos, pode fazer aê"*), o que valida a direção de D-01, D-02 e D-04.
→ *Ainda aberto:* o que exatamente vocês considerariam "validado" ao fim das 4 semanas, e qual o critério para levar o protótipo adiante internamente?

---

## Integração técnica

**12. Transporte.** O TAP recomenda **SSE**, mas o padrão nativo do .NET para tempo real é o **SignalR**. Qual vocês preferem? SSE é mais simples, não adiciona dependência no React e é o que o TAP pede; SignalR é idiomático na stack de vocês e traz reconexão e fallback prontos.
→ *Default se não houver resposta:* SSE, por ser o que o TAP especifica.

**13. Autenticação e planos.** A plataforma tem autenticação e níveis (free/pro)? O módulo precisa respeitar tiers de acesso ou é aberto no protótipo?

**14. Convenções de código.** 🟡 **Parcial:** MVC confirmado em 18/08.
→ *Ainda aberto:* há estrutura de pastas, biblioteca de estado (Redux, Zustand, TanStack Query) ou biblioteca de gráficos obrigatórias no React de vocês, caso queiram absorver o código?

**15. Idioma da interface.** PT-BR, EN, ou ambos?

**16. Carga esperada.** Quantos usuários simultâneos devemos considerar como alvo do protótipo?
→ *Impacta:* RNF-04 (hoje dimensionado para 100 clientes SSE).

---

## Processo

**~~17. Canal e ponto focal.~~** ✅ **Encerrada:** WhatsApp, com **Kadota Manauara** como ponto focal.
→ *Ainda vale confirmar:* há um ponto focal **técnico** separado, para dúvidas de .NET/ClickHouse?

**18. Checkpoint intermediário.** Haverá alguma revisão informal (ex.: fim da semana 2)? O TAP prevê só 2 reuniões, mas 15 minutos no meio do caminho evitam retrabalho.

**19. Repositório.** O repo público MIT fica sob a organização do Inteli Blockchain ou da Alphractal?

**20. Formato da demo.** O que exatamente deve ser demonstrado em 05/10 — rodando localmente ou publicado em algum ambiente (Vercel/Render)?

---

## Stack (definida pela Alphractal em 18/08)

A stack — React, .NET com **estrutura MVC**, Python ETL e ClickHouse — foi definida pelo parceiro. O TAP deixava a escolha livre, então não há conflito. Restam cinco desdobramentos:

**21. Instância de ClickHouse.** Vocês fornecem uma instância para o protótipo, ou subimos local via Docker? Se for a de vocês, há schema, convenção de nomes ou política de retenção a seguir?
→ *Tensão a resolver:* vocês disseram *"salva no banco deles"*, mas o TAP proíbe integração no ambiente de produção e caracteriza a entrega como protótipo em **ambiente isolado**. Nossa decisão provisória é **Docker local espelhando o schema de vocês** ([09 §7](./09-arquitetura-e-stack.md)).
→ *Impacta:* [04 — Persistência](./04-persistencia-banco-de-dados.md), RNF-22, R-20.

**22. Versão e convenções de .NET.** 🟡 **Parcial:** MVC confirmado.
→ *Ainda aberto:* qual versão do .NET vocês usam? Há template de projeto ou bibliotecas internas que devemos seguir?

**23. Nethereum.** Vocês já usam a Nethereum internamente, ou seria a primeira vez? Existe código de ingestão on-chain em .NET do lado de vocês que possamos usar como referência?
→ *Por que importa:* é o maior risco de cronograma do projeto (R-13). O ecossistema Web3 documenta quase tudo em `viem`/`ethers`; um exemplo funcionando de vocês economizaria dias.
→ *Nota:* o pipeline de Dogecoin mencionado em 18/08 pode conter exatamente essa referência — pedir acesso (dúvida 29).

**~~24. Padrão de ETL em Python.~~** ✅ **Encerrada em 18/08:** vocês já operam um pipeline no padrão *ingestão → tratamento → API → front-end*.
→ *Desdobramento:* dúvida 29.

**~~25. Divisão .NET vs Python.~~** ✅ **Encerrada em 18/08:** nossa proposta (.NET no caminho quente, Python no frio) coincide com o padrão interno de vocês.

---

## Novas (a partir da conversa de 18/08)

**26.** 🔴 **"4 métricas misteriosas"** — ver seção de bloqueantes.

**27.** 🔴 **"Nível diário" vs tempo real** — ver seção de bloqueantes.

**28. Convenções de MVC.** Dentro da estrutura MVC, vocês seguem alguma organização específica de pastas, injeção de dependência, validação de entrada ou tratamento de erro que devamos espelhar?
→ *Nossa proposta:* `Controllers/` · `Services/` · `Repositories/` · `Providers/` · `Models/` · `BackgroundServices/`, com o mapeamento das camadas da RNF-14 descrito em [09 §2](./09-arquitetura-e-stack.md).

**29. Acesso ao pipeline existente.** Podemos ver o pipeline de Dogecoin como referência — mesmo que só a estrutura de pastas e o contrato da API, sem o código proprietário? Reduziria bastante o risco R-13 e aumentaria a chance de vocês absorverem o nosso código depois.

---

## Registro de respostas

> Respostas de 18/08/2026 já preenchidas. Transcrição integral em [10 — Registro de Respostas do Parceiro](./10-registro-respostas-parceiro.md).

| # | Resposta | Data | Impacto na spec |
|---|---|---|---|
| 1 | — | | **Bloqueante em aberto** |
| 2 | | | |
| 3 | | | |
| 4 | | | |
| 5 | ClickHouse — *"salva no banco deles"* | 18/08/26 | Doc 04 mantido; abre a dúvida 21 |
| 6 | *"faz aê, sejam livres"* | 18/08/26 | RN-04 deixa de ser *a validar*; faixas em config |
| 7 | Liberdade concedida | 18/08/26 | RN-11 definido pelo time, em config |
| 8 | Liberdade concedida | 18/08/26 | RN-02 = p25/p50/p90 sobre `N_fee` = 20 |
| 9 | | | |
| 10 | *"queremos, pode fazer aê"* (análise de negócio) | 18/08/26 | Valida D-01, D-02, D-04 |
| 11 | *"depois a gente manda alguma coisa"* | 18/08/26 | **Insuficiente — segue bloqueante** |
| 12 | | | |
| 13 | | | |
| 14 | *"estrutura MVC"* | 18/08/26 | RNF-31 criado; [09 §2](./09-arquitetura-e-stack.md) |
| 15 | | | |
| 16 | | | |
| 17 | WhatsApp · Kadota Manauara | 18/08/26 | Ponto focal registrado |
| 18 | | | |
| 19 | | | |
| 20 | | | |
| 21 | — | | Aberta; decisão provisória: Docker local |
| 22 | MVC (parcial) | 18/08/26 | Versão e template ainda em aberto |
| 23 | — | | Aberta; ver dúvida 29 |
| 24 | Pipeline próprio: ingestão → tratamento → API → front | 18/08/26 | Confirma [09 §1](./09-arquitetura-e-stack.md) |
| 25 | Idem | 18/08/26 | Confirma a divisão .NET × Python |
| 26 | — | | **Bloqueante em aberto** |
| 27 | — | | **Bloqueante em aberto** |
| 28 | — | | Aberta |
| 29 | — | | Aberta |
