# Análise de Negócios — Alphractal
### Introdução do Parceiro e Matriz SWOT
*Documento de apoio ao BRD — Módulo "Fees" (Gas Tracker)*

---

## 1. Introdução do Parceiro

### 1.1 Quem é a Alphractal

A **Alphractal** é uma plataforma de inteligência de dados voltada à análise avançada de mercados financeiros, com foco primário em criptoativos e cobertura complementar de ações, índices e indicadores macroeconômicos globais. Fundada em **2023** e sediada em **São Paulo (Brasil)**, a empresa nasceu com a proposta de trazer profundidade e transparência analítica a um mercado historicamente fragmentado entre ferramentas especializadas e caras.

Em pouco mais de dois anos de operação, a companhia consolidou uma base de usuários distribuída em mais de 40 países, composta por gestores de fundos, analistas quantitativos, traders profissionais e criadores de conteúdo de research.

### 1.2 Proposta de valor e portfólio

O posicionamento central da Alphractal é o de **consolidador**: entregar, em uma única plataforma, camadas de dados que o mercado normalmente obriga o investidor a contratar separadamente. A oferta atual se organiza em cinco domínios analíticos:

| Domínio | O que cobre |
|---|---|
| **On-chain** | Fluxo de transações, atividade de endereços, comportamento de holders de longo prazo (SOPR, CDD, HODL Waves) |
| **Derivativos** | Open interest, funding rates, níveis de liquidação, alavancagem |
| **Macroeconômico** | Juros, expectativa de inflação, condições de liquidez global |
| **Sentimento** | Psicologia de mercado, comportamento de manada, tendências sociais |
| **Mercado** | Volume, volatilidade, price action, profundidade de liquidez |

Sobre essa base de dados, a empresa entrega quatro produtos principais:

- **Plataforma web** — dashboards customizáveis (drag-and-drop de gráficos, texto, fórmulas e resumos de IA), screener institucional por ativo e alertas multicondicionais com notificação via e-mail, Telegram e in-app;
- **Alpha AI** — copiloto que responde perguntas de mercado em linguagem natural e gera relatórios de pesquisa a partir do acervo de métricas;
- **API unificada** — endpoints REST e WebSocket voltados a modelos quantitativos, dashboards próprios e integrações;
- **Research & relatórios** — publicações periódicas, incluindo entregas white-label para clientes institucionais.

### 1.3 Modelo de receita

A monetização segue o padrão **SaaS freemium**: camada gratuita de entrada, planos de assinatura individual e um tier institucional (fundos, mesas de trading e empresas) baseado em acesso à API, créditos de consumo, interfaces no-code/SQL para backtesting e relatórios white-label. A empresa também aceita pagamento em criptomoedas, o que reduz o atrito de aquisição em mercados internacionais.

> ⚠️ **A validar com o parceiro:** os valores de assinatura e a estrutura exata de créditos de API mudaram ao longo do tempo. Confirmar a tabela vigente antes de usar em análise financeira/TCO.

### 1.4 Onde o módulo "Fees" se encaixa

O escopo deste projeto — um **tracker de taxas de rede (gas) em tempo real** — não é um acessório periférico: ele endereça uma lacuna direta na camada on-chain da plataforma e reforça a tese central de "uma plataforma no lugar de cinco ferramentas".

Hoje, um usuário institucional da Alphractal que precise avaliar custo de execução ou congestionamento de rede precisa sair da plataforma e recorrer ao Etherscan Gas Tracker, Blocknative ou a dashboards do Dune. O módulo Fees fecha esse ciclo e, mais importante, permite algo que nenhum gas tracker isolado entrega: **cruzar custo de transação com derivativos, sentimento e macro dentro do mesmo ambiente analítico**.

O diferencial estratégico, portanto, não é exibir o preço do gas — dado público e comoditizado —, mas transformá-lo em **sinal de mercado**: congestionamento como proxy de demanda real pela rede, queima de taxas (EIP-1559) como variável de oferta de ETH, e custo comparado entre L1 e L2s como indicador de rotação de capital entre ecossistemas.

---

## 2. Matriz SWOT

### 2.1 Visão consolidada

|  | **Ajuda a atingir o objetivo** | **Atrapalha o objetivo** |
|---|---|---|
| **Origem interna** | **FORÇAS (S)**<br>S1. Amplitude de dados: 1.500+ métricas em 1.000+ ativos, cobrindo 4 domínios em uma só plataforma<br>S2. Alpha AI: camada de IA proprietária que reduz o caminho de dado bruto a insight<br>S3. Infraestrutura pronta: pipelines, API REST/WebSocket, dashboards e motor de alertas já operantes<br>S4. Preço agressivo + freemium frente aos incumbentes<br>S5. Reputação analítica na comunidade (inclusive colaborações com a CryptoQuant) e distribuição orgânica via research<br>S6. Ciclo de release rápido e time enxuto | **FRAQUEZAS (W)**<br>W1. Marca jovem (2023) — ciclo de venda institucional depende de histórico e confiança<br>W2. Dependência de fontes de terceiros (exchanges, provedores RPC) para ingestão de dados<br>W3. Defasagem de documentação: ~270 indicadores documentados publicamente vs. 1.500+ anunciados<br>W4. Arquitetura otimizada para frequências diária/horária — fees exigem granularidade de bloco<br>W5. Ausência de métricas de custo de transação na oferta atual (a lacuna que o projeto ataca)<br>W6. Concentração de conhecimento em time pequeno (*bus factor*) |
| **Origem externa** | **OPORTUNIDADES (O)**<br>O1. Fees como sinal de mercado, não utilitário — território não ocupado pelos concorrentes<br>O2. Explosão de L2s/rollups cria demanda por comparação de custo entre redes<br>O3. Monetização institucional: dado de fee em tempo real via WebSocket justifica tier premium e eleva ARPU<br>O4. Institucionalização e regulação do mercado cripto elevam a demanda por analytics auditável<br>O5. Mercado brasileiro subatendido — plataforma nacional, suporte e research em PT-BR<br>O6. Alertas de gas como *feature* de uso diário → retenção e redução de churn<br>O7. Parceria acadêmica (Inteli) como P&D de baixo custo e pipeline de talentos | **AMEAÇAS (T)**<br>T1. Incumbentes capitalizados (Glassnode, Nansen, CryptoQuant, Dune, Messari) podem replicar o módulo rapidamente<br>T2. Comoditização: dado de gas é público e gratuito (Etherscan, Blocknative, RPCs abertos)<br>T3. Custo e rate limit de provedores RPC (Alchemy, Infura, QuickNode) comprimindo margem<br>T4. Ciclicidade do mercado cripto: bear market reduz assinaturas e atividade on-chain simultaneamente<br>T5. Mudanças de protocolo (EIP-4844/blobs, upgrades futuros) alteram a semântica das taxas e quebram séries históricas<br>T6. Risco regulatório e tributário sobre serviços de dados cripto |

> **Atualização relevante — O1 e T2.** A **Blocknative encerrou suas APIs em 19/06/2026**, após aquisição de talentos pela Deloitte. Era o único player do nicho com camada analítica real (previsão via mempool e regressão quantílica). Isso **amplifica O1** — abriu-se um vácuo de oferta com base institucional em migração forçada — e ao mesmo tempo **valida T2**: a empresa optou por desligar em vez de vender, evidência de que o dado bruto de gas, isolado, não se sustenta comercialmente. Detalhamento nos documentos 03 e 04.

### 2.2 Leitura estratégica (cruzamento TOWS)

O valor da SWOT está menos na lista e mais no que se faz com o cruzamento dos quadrantes:

| Cruzamento | Estratégia derivada |
|---|---|
| **S1+S2 × O1** *(ofensiva)* | Posicionar o módulo como **"Fees Intelligence"**, não como gas tracker. Cruzar gas com open interest, funding e macro é algo que Etherscan e Blocknative estruturalmente não conseguem fazer — é aqui que mora a defensabilidade. |
| **W4 × O3** *(reforço)* | Construir a camada de streaming do módulo Fees como **fundação reutilizável** de tempo real para toda a plataforma. O custo de arquitetura se amortiza em métricas futuras, não só neste módulo. |
| **S3 × T1** *(velocidade)* | A infraestrutura existente permite entregar o MVP em semanas, não trimestres. Contra concorrentes maiores e mais lentos, o *time-to-market* é a arma. |
| **S5 × T2** *(diferenciação)* | Neutralizar a comoditização com **métricas proprietárias derivadas**: percentis de gas, índice de congestionamento, custo efetivo por tipo de operação (transfer/swap/mint), fee burn normalizado. Ninguém paga pelo dado bruto; paga-se pela interpretação. |
| **W2 × T3** *(defesa)* | Arquitetar com **multi-provider fallback** e cache agressivo desde o início. Não amarrar o módulo a um único RPC — é simultaneamente mitigação de risco técnico e controle de TCO. |
| **W1 × O4/O5** *(posicionamento)* | Usar o mercado brasileiro e o research público como prova social para encurtar o ciclo de venda institucional, enquanto a marca amadurece. |

### 2.3 Implicações diretas para o escopo do MVP

Traduzindo a leitura acima em recomendações de escopo:

1. **Priorizar profundidade analítica sobre cobertura de redes.** Ethereum + 1 ou 2 L2s relevantes, bem instrumentados, valem mais que dez chains rasas — é a profundidade que diferencia dos trackers gratuitos.
2. **Entregar histórico, não só tempo real.** O dado instantâneo é comoditizado; a série histórica com percentis e sazonalidade é o que sustenta análise.
3. **Alertas desde o MVP.** É a feature de menor custo e maior impacto em retenção (O6).
4. **Multi-provider desde o dia um** (W2 × T3), com métricas de custo por requisição instrumentadas para alimentar a análise de TCO.
5. **Blindar a semântica das taxas** contra upgrades de protocolo (T5): versionar a metodologia de cálculo e documentar quebras de série.

---

## 3. Premissas e pontos a validar com o parceiro

Esta análise foi construída a partir de fontes públicas e do documento de requisitos do projeto. Os pontos abaixo precisam de confirmação antes de fechar o BRD:

- [ ] A Alphractal já possui alguma métrica de fees/gas em produção, ainda que limitada?
- [ ] Quais redes são prioritárias (Ethereum apenas, L2s, Solana, Bitcoin)?
- [ ] Qual provedor RPC será usado e quais são os limites do plano contratado?
- [ ] O módulo será exposto no tier gratuito, pago ou institucional?
- [ ] Qual a latência aceitável — atualização por bloco, por segundo, por minuto?
- [ ] Há necessidade de *backfill* histórico? De quanto tempo?
- [ ] A tabela de preços e a estrutura de créditos de API vigentes (para a análise financeira/TCO).

---

## 4. Fontes

- Site institucional Alphractal — https://alphractal.com/
- Página institucional / oferta enterprise — https://www.alphractal.com/institucional
- Documentação de métricas — https://alphractal.github.io/metrics-docs/
- Guia da plataforma e API — https://docs.alphractal.com/
- Perfil corporativo (ZoomInfo) — https://www.zoominfo.com/c/alphractal/1340767540
- Canais de research e atualizações de produto — @Alphractal (X) e t.me/alphractal

*Documento elaborado em agosto de 2026. Dados de produto e precificação sujeitos a alteração — reconfirmar antes da entrega final.*
