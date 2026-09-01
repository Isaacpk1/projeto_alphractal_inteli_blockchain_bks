# Business Model Canvas — Alphractal
### Com destaque do encaixe do módulo "Fees"

**Legenda:** ⛽ = bloco impactado diretamente pelo módulo Fees | ⚠️ = premissa a validar com o parceiro

---

## Visão consolidada

| | |
|---|---|
| **8. Parcerias-chave**<br>• Provedores de RPC e nós (Alchemy, Infura, QuickNode) ⛽<br>• Exchanges centralizadas como fonte de dados de derivativos e spot<br>• Provedores de dado macroeconômico<br>• CryptoQuant e comunidade de research (colaboração e credibilidade)<br>• Canais digitais de distribuição de conteúdo<br>• Instituições de ensino (Inteli) como P&D e pipeline de talentos ⛽<br>• Processadores de pagamento fiat e cripto | **7. Atividades-chave**<br>• Ingestão e normalização de dados multi-fonte ⛽<br>• Pesquisa e criação de métricas proprietárias ⛽<br>• Desenvolvimento e operação da plataforma e da API ⛽<br>• Produção de research e conteúdo (motor de aquisição)<br>• Desenvolvimento do copiloto de IA<br>• Suporte e relacionamento institucional |
| **6. Recursos-chave**<br>• Acervo de 1.500+ métricas em 1.000+ ativos<br>• Base histórica de séries temporais ⛽<br>• Infraestrutura de ingestão, API REST/WebSocket e motor de alertas ⛽<br>• Modelo de IA proprietário (Alpha AI)<br>• Marca e reputação analítica na comunidade<br>• Time de engenharia de dados e research | **2. Proposta de valor**<br>**"A stack analítica que substitui cinco ferramentas."**<br><br>• Consolidação de on-chain, derivativos, sentimento e macro em um só lugar<br>• Profundidade a preço acessível frente aos incumbentes<br>• Insight em linguagem natural via Alpha AI<br>• Métricas proprietárias indisponíveis em outras plataformas<br><br>**Contribuição do módulo Fees** ⛽<br>• Fecha a última grande lacuna da camada on-chain<br>• Transforma custo de rede de utilitário em **sinal de mercado**<br>• Único ambiente onde gas é cruzável com derivativos e macro |
| **4. Relacionamento com clientes**<br>• Self-service com onboarding guiado (freemium)<br>• Alertas e notificações como vínculo recorrente ⛽<br>• Research periódico e comunidade em canais digitais<br>• Atendimento consultivo no tier institucional<br>• Suporte e conteúdo em português — diferencial no mercado nacional | **1. Segmentos de clientes**<br>**Primário institucional:** fundos, mesas de trading, tesourarias<br>**Primário profissional:** analistas quantitativos, gestores independentes<br>**Secundário:** traders ativos e criadores de research<br>**Entrada:** investidores individuais no tier gratuito<br><br>**Segmento novo aberto pelo Fees** ⛽<br>• Times de execução e operações on-chain<br>• Órfãos da descontinuação da Blocknative (jun/2026) |
| **3. Canais**<br>• Plataforma web e aplicativo<br>• API (canal e produto simultaneamente) ⛽<br>• Research e conteúdo orgânico como topo de funil<br>• Canais digitais de relacionamento e retenção<br>• Venda consultiva direta no institucional<br>• Relatórios white-label (canal indireto) | |
| **9. Estrutura de custos**<br>• Provedores de dados e RPC — **custo variável, principal alavanca do módulo Fees** ⛽<br>• Armazenamento de série temporal (cresce com granularidade) ⛽<br>• Computação e hospedagem<br>• Inferência de IA<br>• Time de engenharia e research (maior custo fixo)<br>• Marketing e aquisição<br>• Processamento de pagamentos | **5. Fontes de receita**<br>• Assinatura recorrente individual (mensal e anual)<br>• Tier institucional: API, no-code/SQL, backtesting<br>• Créditos de consumo de API ⛽<br>• Relatórios white-label<br>• Pagamento aceito em cripto (reduz atrito internacional)<br><br>**Alavanca do módulo Fees** ⛽<br>• Dado de tempo real como justificativa de tier premium<br>• Alertas de custo elevam retenção e reduzem churn |

---

## Leitura estratégica dos blocos

### Onde o módulo Fees cria valor

**Proposta de valor (2) — é aqui que o módulo importa mais.** A tese comercial da Alphractal é consolidação. Cada lacuna de dado que força o usuário a abrir outra aba é uma erosão dessa tese. Gas é hoje a lacuna mais visível na camada on-chain: é o dado que qualquer operador consulta várias vezes ao dia. Fechá-la não adiciona uma feature — repara a promessa central do produto.

**Fontes de receita (5).** O dado de gas em tempo real tem uma propriedade rara no portfólio atual: ele é *operacionalmente* necessário, não apenas analiticamente interessante. Isso o torna candidato natural a gatilho de upgrade de tier, e o consumo via API é mensurável e cobrável por crédito.

**Relacionamento (4).** Alertas de gas são a feature de maior frequência de uso possível dentro da plataforma — o usuário volta todo dia, não toda semana. É a alavanca mais barata de retenção disponível.

### Onde o módulo cria pressão

**Estrutura de custos (9) — o ponto de atenção.** Diferentemente de métricas de frequência diária, dados de taxa exigem coleta por bloco. Em Ethereum são cerca de 7.200 blocos por dia; em L2s de bloco rápido, a ordem de grandeza sobe uma ou duas casas. Isso transforma um custo até então marginal em uma linha visível de despesa variável, diretamente proporcional à granularidade e ao número de redes.

**Parcerias-chave (8).** A dependência de provedores de RPC deixa de ser conveniência e passa a ser risco de continuidade de negócio. A descontinuação da Blocknative em junho de 2026 é a demonstração empírica: um fornecedor de referência do setor pode simplesmente desligar. Nenhum bloco crítico do canvas deve depender de um único fornecedor.

---

## Encaixe do módulo — resumo em uma frase por bloco

| Bloco | Contribuição do Fees |
|---|---|
| 1. Segmentos | Abre acesso a times de execução e captura usuários órfãos da Blocknative |
| 2. Proposta de valor | Fecha a lacuna que mais contradizia a promessa de consolidação |
| 3. Canais | Reforça a API como produto autônomo |
| 4. Relacionamento | Alerta de gas = maior frequência de retorno de toda a plataforma |
| 5. Receita | Gatilho de upgrade de tier e consumo mensurável por crédito |
| 6. Recursos | Cria a camada de streaming, reaproveitável por outras métricas |
| 7. Atividades | Adiciona operação contínua de ingestão em alta frequência |
| 8. Parcerias | Eleva a criticidade da relação com provedores de RPC |
| 9. Custos | Introduz custo variável proporcional a redes × granularidade |

---

## Hipóteses do modelo a validar

| # | Hipótese | Como testar |
|---|---|---|
| H1 | Usuários efetivamente saem da plataforma para consultar gas | Analytics de sessão; entrevista com usuários |
| H2 | Dado de gas integrado justifica upgrade de tier | Teste de disposição a pagar; oferta A/B |
| H3 | Alertas de gas aumentam frequência de retorno | Coorte com e sem alerta ativo |
| H4 | Existe demanda residual da base órfã da Blocknative | Pesquisa de canal; conteúdo direcionado à migração |
| H5 | O custo variável de coleta escala de forma aceitável | Medição real no MVP — ver documento 06 |
