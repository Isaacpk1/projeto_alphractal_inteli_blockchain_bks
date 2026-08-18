[← Índice](./README.md)

# 06 — Dúvidas para o Kick-off (14/09/2026)

> O TAP prevê apenas **2 reuniões obrigatórias** com o parceiro. Tudo que não for resolvido em 14/09 vira e-mail com SLA de 48 h úteis — o que pode custar dias de trabalho.
> **As dúvidas 1, 3, 6 e 11 são bloqueantes**: sem elas não dá para começar a semana 2.

---

## 🔴 Bloqueantes

**1. Provedor RPC e chave de API.** Qual provedor usaremos e **quem fornece a chave** — a Alphractal disponibiliza uma conta Alchemy/Infura ou trabalhamos no plano gratuito? Qual o limite de *compute units* aceitável?
→ *Impacta:* RF-01, RNF-05, e a viabilidade de D-06.

**3. Mempool real ou métricas derivadas?** Vocês querem dados reais de transações pendentes, ou basta `baseFee` + `eth_feeHistory`?
→ *Contexto a explicar na reunião:* assinar `pendingTransactions` é caro e costuma ser bloqueado em planos básicos. Se for requisito real, muda arquitetura e custo. *Impacta:* RF-07.

**6. Definição de "saúde da rede".** Como a Alphractal define isso hoje? Existem faixas/limiares já usados por vocês, ou definimos nós?
→ *Impacta:* RN-04. *Alternativa que propomos:* substituir limiares arbitrários por percentil histórico (D-02), que é estatisticamente mais honesto.

**11. Design e identidade visual.** Existe Figma, design system ou tokens de cor da Alphractal que possamos usar para o painel se integrar à aba "Fees"? Podemos receber prints da aba atual?
→ *Impacta:* a entrega da semana 1 (protótipo de alta fidelidade) trava sem isso.

---

## Dados e infraestrutura

**2. Escopo de redes.** Somente Ethereum Mainnet, ou o painel já deve ser preparado para L2s (Base, Arbitrum, Optimism)? Desenvolvemos contra Sepolia ou direto na Mainnet — leitura é gratuita?

**4. Fonte de preço ETH/USD.** Já existe internamente um serviço de preço na plataforma que devemos consumir, ou escolhemos a fonte (CoinGecko, Binance, Chainlink)?

**5. Histórico e banco de dados.** Vocês já têm o histórico de fees na plataforma — o protótipo deve **persistir** os dados que capturar, e por quanto tempo? Há preferência de banco/infra já usada por vocês (Postgres, Timescale, Redis)? Faria mais sentido o painel consumir o histórico longo do backend de vocês em vez de manter o próprio?
→ *Impacta:* toda a seção [04](./04-persistencia-banco-de-dados.md), e habilita D-02 e D-04.

---

## Produto e regras

**7. Tipos de transação relevantes.** Quais importam para o usuário institucional de vocês (swap, bridge, transferência de stablecoin, mint, aprovação)? Há *gas limits* de referência que vocês já usam?
→ *Impacta:* RN-11.

**8. Padrão das faixas de velocidade.** Lento/Padrão/Rápido devem seguir algum padrão específico (ex.: igual ao Etherscan Gas Tracker) ou temos liberdade nos percentis?
→ *Impacta:* RN-02.

**9. Alertas.** O painel deve incluir alertas/notificações — visual apenas, ou também e-mail/Telegram? Está no MVP ou é evolução futura?
→ *Impacta:* RF-30, D-12.

**10. Métrica de sucesso.** O que vocês consideram "validado" ao fim das 4 semanas? Qual seria o critério para levar o protótipo adiante internamente?

---

## Integração técnica

**12. Transporte.** Vocês preferem **SSE** (como sugerido no TAP) ou o padrão de vocês no frontend é WebSocket? Isso impacta a integração futura.

**13. Autenticação e planos.** A plataforma tem autenticação e níveis (free/pro)? O módulo precisa respeitar tiers de acesso ou é aberto no protótipo?

**14. Convenções de código.** Há estrutura de pastas, biblioteca de estado ou biblioteca de gráficos obrigatórias, caso vocês queiram absorver o código depois?

**15. Idioma da interface.** PT-BR, EN, ou ambos?

**16. Carga esperada.** Quantos usuários simultâneos devemos considerar como alvo do protótipo?
→ *Impacta:* RNF-04.

---

## Processo

**17. Canal e ponto focal.** WhatsApp ou e-mail para dúvidas assíncronas? Quem é o ponto focal técnico do lado de vocês?

**18. Checkpoint intermediário.** Haverá alguma revisão informal (ex.: fim da semana 2)? O TAP prevê só 2 reuniões, mas 15 minutos no meio do caminho evitam retrabalho.

**19. Repositório.** O repo público MIT fica sob a organização do Inteli Blockchain ou da Alphractal?

**20. Formato da demo.** O que exatamente deve ser demonstrado em 05/10 — rodando localmente ou publicado em algum ambiente (Vercel/Render)?

---

## Registro de respostas

| # | Resposta | Data | Impacto na spec |
|---|---|---|---|
| 1 | | | |
| 2 | | | |
| 3 | | | |
| 4 | | | |
| 5 | | | |
| 6 | | | |
| 7 | | | |
| 8 | | | |
| 9 | | | |
| 10 | | | |
| 11 | | | |
| 12 | | | |
| 13 | | | |
| 14 | | | |
| 15 | | | |
| 16 | | | |
| 17 | | | |
| 18 | | | |
| 19 | | | |
| 20 | | | |
