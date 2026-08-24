# Benchmarking Competitivo
### Gas trackers e plataformas de analytics cripto — agosto de 2026

---

## 1. Recorte da análise

O benchmarking cobre dois grupos que raramente são comparados entre si, mas que disputam a mesma atenção do usuário:

- **Grupo A — Ferramentas de gas:** especializadas em custo de transação. Concorrentes diretos do módulo.
- **Grupo B — Plataformas de analytics:** concorrentes da Alphractal como um todo, e potenciais entrantes no nicho de gas.

---

## 2. Grupo A — Ferramentas especializadas em gas

| Ferramenta | Modelo | Cobertura | Tempo real | Histórico | API | Camada analítica | Preço |
|---|---|---|---|---|---|---|---|
| **Etherscan Gas Tracker** | Explorer + API | Ethereum + rede Etherscan; a V2 unificou 60+ cadeias EVM sob uma única chave | Sim | Blocos recentes | Sim, com endpoint de gas oracle | Baixa — mostra o número e o custo em dApps populares | Gratuito; API com tiers |
| **Owlracle** | API de gas multichain | Ampla — Ethereum, BSC, Polygon, Base, Arbitrum, Optimism, Linea e outras | Sim | Limitado | Sim | Baixa | Gratuito / freemium |
| **Blocknative Gas Platform** | API de previsão via mempool | 40+ redes | Sim | Sim | Sim | Alta — regressão quantílica, previsão de base fee e blob fee para 5 blocos à frente | **❌ Descontinuado em 19/06/2026** |
| **Estimadores de carteira** | Embutido | Rede da carteira | Sim | Não | Não | Nenhuma | Gratuito |
| **Dashboards públicos no Dune** | SQL comunitário | 100+ cadeias | Não (dado por consulta) | Sim | Sim, em todos os tiers | Depende de quem escreveu | Gratuito |

### 2.1 O evento que redefine o grupo

A **Blocknative encerrou suas APIs em 19 de junho de 2026**, após aquisição de talentos pela Deloitte. A Gas Network, oráculo descentralizado de precificação de gas que dependia da mesma infraestrutura, foi desligada na mesma data.

Era a ferramenta tecnicamente mais sofisticada do grupo: fundada em 2018, previa o preço mínimo de inclusão no próximo bloco com base em leitura de mempool em tempo real e modelo de regressão quantílica, com níveis de confiança configuráveis — algo que oráculos baseados apenas em blocos passados não conseguem replicar.

**Duas leituras, ambas relevantes para este projeto:**

*Oportunidade.* Saiu do tabuleiro o único player com camada analítica de fato no grupo A. Sobram ferramentas que mostram o número sem interpretá-lo. E há uma base de usuários institucionais em migração forçada.

*Alerta.* A decisão de **desligar em vez de vender** — a infraestrutura tinha valor técnico evidente e não encontrou comprador — é evidência direta de que o mercado de dados de gas isolado é comercialmente frágil. O encerramento veio em meio a uma onda de mais de vinte empresas cripto reestruturando ou fechando no trimestre anterior.

### 2.2 Lacuna consolidada do Grupo A

| Capacidade | Alguém entrega? |
|---|---|
| Gas atual em tempo real | ✅ Vários |
| Cobertura multichain ampla | ✅ Owlracle, Etherscan V2 |
| Previsão de próximo bloco | ⚠️ Ninguém desde jun/2026 |
| Série histórica longa e íntegra | ⚠️ Parcial |
| Análise do mercado de blobs (EIP-4844) | ❌ Ninguém |
| Custo por operação econômica (swap, mint) | ⚠️ Etherscan, de forma rasa |
| Cruzamento com dados de mercado | ❌ **Ninguém** |
| Alertas configuráveis por limiar | ❌ Praticamente ninguém |

**As três últimas linhas são o espaço do projeto.**

---

## 3. Grupo B — Plataformas de analytics cripto

| Plataforma | Especialidade | Escala de dados | API | Faixa de preço |
|---|---|---|---|---|
| **Glassnode** | Métricas on-chain pré-computadas para BTC e ETH; expandiu para derivativos e opções | Mais de 800 métricas só para Bitcoin, incluindo variantes ajustadas por entidade | API completa apenas no tier Professional | A partir de US$ 49/mês no anual (US$ 99 no mensal); tiers profissionais até US$ 999/mês |
| **Nansen** | Rotulagem de carteiras e rastreamento de "smart money" | Cerca de 300 milhões de endereços rotulados | Modelo por chamada, cerca de US$ 0,01, liquidado em USDC | Faixa de US$ 99 a US$ 1.299/mês |
| **Dune** | Acesso SQL bruto e dashboards comunitários | Mais de 100 blockchains | Em todos os tiers, inclusive gratuito; consumo por créditos de computação | Gratuito + planos pagos |
| **CryptoQuant** | Fluxos de exchange e métricas on-chain | BTC, ETH e principais ativos | A partir do tier Professional | A partir de US$ 99/mês no Professional |
| **Messari** | Research + dado estruturado | Ampla | Em todos os tiers, com limites por tier | Lite a Enterprise |
| **Alphractal** | Consolidação multi-domínio + IA | Mais de 1.500 métricas em mais de 1.000 ativos | REST e WebSocket | Faixa acessível ⚠️ *(reconfirmar tabela vigente)* |

### 3.1 Posição relativa da Alphractal

| Dimensão | Posição | Comentário |
|---|---|---|
| Amplitude de domínios | 🟢 **Líder** | Poucos combinam on-chain + derivativos + sentimento + macro |
| Profundidade por domínio | 🟡 Intermediária | Glassnode tem mais profundidade em BTC; Nansen, em rotulagem |
| Preço | 🟢 **Vantagem clara** | Bem abaixo dos tiers profissionais dos incumbentes |
| Maturidade de marca | 🔴 **Desvantagem** | Fundada em 2023, contra players com anos de histórico institucional |
| Camada de IA | 🟢 Vantagem | Copiloto em linguagem natural, ainda não padrão no setor |
| Acesso à API | 🟡 A confirmar | Dune libera em todos os tiers; Glassnode trava a API completa no tier de US$ 999 |
| **Dados de gas** | 🔴 **Lacuna** | Foco deste projeto |

### 3.2 Como cada plataforma trata gas hoje

Nenhuma das plataformas do Grupo B trata taxa de rede como métrica de primeira classe. Glassnode e CryptoQuant tocam o tema por métricas de receita de mineradores e validadores; Dune permite construir qualquer coisa em SQL, sem tempo real nem curadoria; Nansen foca em atribuição de entidade, não em economia de rede.

**Ninguém no Grupo B ocupa o espaço. Ninguém no Grupo A tem os dados para ocupá-lo.** É uma lacuna de mercado real — o que também significa que ela pode simplesmente refletir ausência de demanda, e por isso as hipóteses do documento 02 precisam ser testadas.

---

## 4. Matriz de posicionamento

Eixo X: profundidade em dados de gas → | Eixo Y: integração com dados de mercado ↑

```
     alta │                                    ★ ALPHRACTAL + FEES
integração│                                          (alvo)
  com     │  ● Glassnode
 mercado  │  ● Nansen        ● Dune
          │  ● CryptoQuant
          │                                    ● Etherscan
          │                                    ● Owlracle
          │                              ✕ Blocknative (jun/2026)
     baixa└────────────────────────────────────────────────────
           baixa          profundidade em gas           alta
```

O quadrante superior direito está vazio — e ficou ainda mais vazio com a saída da Blocknative, que ocupava a base do eixo de profundidade sem nunca ter subido no eixo de integração.

---

## 5. Análise de gaps e ações

| Gap identificado | Concorrente que expõe | Ação recomendada | Prioridade |
|---|---|---|---|
| Ausência de qualquer métrica de gas | Etherscan, Owlracle | Escopo central do MVP | Crítica |
| Previsão de próximo bloco sem oferta no mercado | Vácuo pós-Blocknative | Avaliar para fase 2; exige mempool ou modelo estatístico sobre blocos | Média |
| Mercado de blobs sem cobertura | Ninguém | Diferencial de baixo custo — o dado já vem no bloco | **Alta** |
| Alertas de gas praticamente inexistentes | Ninguém | Reaproveitar motor de alertas existente | **Alta** |
| Cobertura multichain inferior | Owlracle, Dune | **Não perseguir** — competir em profundidade, não em amplitude | Baixa |
| Maturidade de marca institucional | Glassnode, Nansen | Metodologia publicada + research como prova social | Média |

---

## 6. Conclusões

**1. O nicho de gas está tecnicamente mal servido e comercialmente comprometido.** As ferramentas restantes entregam o número sem contexto. A única que entregava contexto fechou. Isso é ao mesmo tempo a oportunidade e o alerta.

**2. A vantagem competitiva sustentável não está no dado.** Está na posse simultânea de gas, derivativos, fluxo e macro. Nenhum concorrente do Grupo A pode replicar isso sem construir uma plataforma inteira; nenhum do Grupo B demonstrou interesse em descer para o nível de dado de rede.

**3. Cobertura de redes é uma armadilha.** Owlracle e Dune vencem essa disputa e ela não gera receita. Duas ou três redes bem instrumentadas valem mais que quinze rasas.

**4. Blobs são a oportunidade de melhor relação custo-benefício.** O dado já vem no mesmo bloco que se está coletando, ninguém o analisa e ele é economicamente relevante para toda a tese de L2.

**5. Alertas são a alavanca de retenção mais barata disponível.** O motor já existe na plataforma; o custo marginal é próximo de zero e nenhum concorrente do Grupo A oferece.

---

## 7. Limitações desta análise

- Preços de concorrentes mudam com frequência e foram levantados em fontes secundárias — reconfirmar antes da apresentação final.
- A avaliação de profundidade de cobertura de gas nas plataformas do Grupo B foi feita por documentação pública, sem acesso a contas pagas.
- A posição atual da Alphractal em métricas de gas depende de confirmação com o parceiro. ⚠️
- Não foram avaliados players regionais asiáticos, que podem ter oferta relevante não capturada.
