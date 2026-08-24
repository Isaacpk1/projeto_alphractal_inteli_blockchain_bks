# BRD — Business Requirements Document
## Módulo "Fees" — Rastreamento e Inteligência de Taxas de Rede
**Parceiro:** Alphractal | **Versão:** 1.0 | **Data:** agosto de 2026

---

## 1. Sumário executivo

A Alphractal é uma plataforma de inteligência de dados para mercados cripto que consolida métricas on-chain, de derivativos, de sentimento e macroeconômicas em um único ambiente. Sua tese comercial é substituir a necessidade de contratar múltiplas ferramentas especializadas.

Existe hoje uma lacuna nessa consolidação: **dados de custo de transação (gas)**. Um usuário institucional que precise avaliar custo de execução, congestionamento de rede ou dinâmica de queima de ETH precisa sair da plataforma. Este projeto endereça essa lacuna com um módulo de coleta, cálculo e visualização de métricas de taxas de rede em tempo quase real.

O diferencial proposto não é exibir o preço do gas — dado público e gratuito —, mas **tratar a taxa de rede como indicador de mercado**, cruzável com as demais camadas de dados já existentes na plataforma. Essa distinção é o eixo de todo o documento.

---

## 2. Contexto e problema de negócio

### 2.1 Situação atual

| Aspecto | Estado |
|---|---|
| Cobertura de dados de gas na plataforma | Ausente ou marginal ⚠️ *(confirmar com o parceiro)* |
| Comportamento do usuário | Sai da plataforma para Etherscan, Owlracle ou dashboards do Dune |
| Oferta de mercado | Fragmentada entre trackers gratuitos rasos e infraestrutura para desenvolvedores |
| Referência histórica do setor | **Descontinuada** — a Blocknative encerrou APIs em 19/06/2026 |

### 2.2 O problema

Três problemas distintos se sobrepõem:

**P1 — Quebra de fluxo analítico.** O usuário que analisa uma tese na Alphractal precisa mudar de ferramenta para responder "quanto custa executar isso agora?". Cada saída da plataforma é uma oportunidade de churn e uma contradição com a proposta de valor de consolidação.

**P2 — Dado de gas sem camada analítica.** As ferramentas existentes respondem "qual o gas agora?" mas não "o que esse gas está dizendo sobre o mercado?". Congestionamento é proxy de demanda real pela rede; queima via EIP-1559 é variável de oferta de ETH; diferencial de custo L1 vs L2 é indicador de rotação de capital. Nenhum tracker isolado consegue cruzar isso com open interest, funding rate ou fluxo de exchanges — porque não possui esses dados.

**P3 — Vácuo de oferta institucional.** Com a saída da Blocknative, uma base de usuários corporativos que consumia gas data de qualidade profissional está em migração forçada. A janela é curta e o espaço, temporariamente desocupado.

### 2.3 Por que agora

- O vácuo deixado pela Blocknative é conjuntural e será preenchido — a vantagem é de quem chega primeiro.
- A proliferação de L2s tornou a comparação de custo entre redes uma decisão recorrente, e não há ferramenta boa para isso.
- A infraestrutura da Alphractal (pipelines, API, motor de dashboards e alertas) já existe: o custo marginal de adicionar o módulo é baixo.

---

## 3. Objetivos

### 3.1 Objetivo geral

Desenvolver e validar um protótipo funcional de módulo de inteligência de taxas de rede, integrado à plataforma Alphractal, capaz de coletar dados de gas em tempo quase real, derivar métricas analíticas proprietárias e expô-las de forma consumível por dashboards, alertas e API.

### 3.2 Objetivos específicos

| ID | Objetivo | Métrica de verificação |
|---|---|---|
| OE-01 | Coletar e persistir dados de taxa por bloco de forma confiável | ≥ 99,5% de cobertura de blocos no período de teste |
| OE-02 | Entregar métricas derivadas que não existam nos concorrentes | ≥ 4 métricas proprietárias documentadas |
| OE-03 | Manter latência compatível com uso operacional | Dado disponível ≤ 1 bloco após confirmação |
| OE-04 | Operar dentro de envelope de custo previsível | Custo de infra dentro do teto definido no doc. 06 |
| OE-05 | Garantir explicabilidade das métricas | 100% das métricas com fórmula e fonte documentadas |
| OE-06 | Integrar às camadas de dado existentes | ≥ 1 visualização cruzando gas com métrica de outro domínio |

### 3.3 Não-objetivos

Explicitar o que o projeto **não** busca evita expectativa mal calibrada:

- Não é objetivo prever o preço do gas com modelos preditivos complexos (pode ser evolução futura).
- Não é objetivo competir em cobertura de redes com trackers multichain generalistas.
- Não é objetivo substituir infraestrutura de execução de transações (o produto é analítico, não transacional).
- Não é objetivo construir monitoramento de mempool próprio — foi exatamente o custo que inviabilizou a Blocknative.

---

## 4. Escopo

### 4.1 Dentro do escopo (MVP)

**Coleta**
- Ingestão de dados por bloco: `baseFeePerGas`, `gasUsed`, `gasLimit`, distribuição de `priorityFeePerGas`, `blobBaseFee`
- Ethereum mainnet + 1 a 2 L2s prioritárias ⚠️ *(definir quais com o parceiro)*
- Persistência em série temporal com backfill histórico

**Métricas derivadas**
- Percentis de priority fee (p10 / p50 / p90) por janela configurável
- Índice de congestionamento (`gasUsed / gasLimit`)
- Custo estimado por tipo de operação: transferência simples, swap ERC-20, mint
- Fee burn acumulado (EIP-1559) e emissão líquida de ETH
- Diferencial de custo L1 vs L2

**Entrega**
- Widget para o construtor de dashboards existente
- Série histórica navegável
- Regra de alerta por limiar (ex.: "notificar quando base fee < X gwei")
- Endpoint de API para consumo externo

### 4.2 Fora do escopo

| Item | Justificativa |
|---|---|
| Monitoramento de mempool próprio | Custo de infraestrutura desproporcional ao horizonte de 10 semanas |
| Previsão de gas por machine learning | Depende de base histórica consolidada; candidato a fase 2 |
| Redes não-EVM (Bitcoin, Solana) | Semântica de taxa distinta exige modelagem própria |
| Execução ou roteamento de transações | Fora da natureza analítica do produto |
| Análise de MEV | Domínio adjacente, escopo próprio |
| Migração de clientes órfãos da Blocknative | Ação comercial, não técnica |

### 4.3 Premissas

| ID | Premissa | Impacto se falsa |
|---|---|---|
| PR-01 | O parceiro fornece credenciais de provedor RPC ou autoriza uso de plano gratuito | Bloqueia coleta; projeto inviável sem alternativa |
| PR-02 | Há acesso a ambiente de persistência (banco de série temporal) | Exige provisionamento próprio, consome tempo de sprint |
| PR-03 | O motor de dashboards e alertas existente aceita novas fontes de métrica | Exige desenvolvimento de front-end adicional |
| PR-04 | Dados históricos podem ser obtidos por backfill via RPC | Reduz profundidade histórica do MVP |
| PR-05 | Definição de redes prioritárias até o fim da Sprint 1 | Retrabalho de arquitetura |

### 4.4 Restrições

- **Prazo:** 10 semanas, em sprints supervisionados
- **Equipe:** time de estudantes, dedicação parcial
- **Orçamento:** infraestrutura preferencialmente em camada gratuita ou de baixo custo
- **Dados:** apenas fontes públicas on-chain e provedores RPC contratados
- **Blocknative indisponível** como fonte de dados

---

## 5. Stakeholders

| Stakeholder | Papel | Interesse | Influência |
|---|---|---|---|
| Alphractal — liderança de produto | Patrocinador | Fechar lacuna competitiva e reforçar tese de consolidação | Alta |
| Alphractal — engenharia de dados | Validador técnico | Integração sem dívida técnica; custo de operação controlado | Alta |
| Usuários institucionais (fundos, mesas) | Usuário final primário | Custo de execução e sinal de mercado no mesmo ambiente | Média |
| Analistas e criadores de research | Usuário final secundário | Material para publicações e teses | Média |
| Traders individuais (tier gratuito) | Usuário final | Timing de transação, economia de custo | Baixa |
| Equipe de alunos | Executor | Entrega no prazo e aprendizado | Alta |
| Orientação acadêmica (Inteli) | Governança | Rigor metodológico e aderência ao escopo | Média |

---

## 6. Requisitos de negócio

| ID | Requisito | Prioridade |
|---|---|---|
| RN-01 | O módulo deve reduzir a necessidade de o usuário sair da plataforma para consultar custo de rede | Must |
| RN-02 | O módulo deve entregar interpretação analítica, não apenas o dado bruto de gas | Must |
| RN-03 | As métricas devem ser cruzáveis com as demais camadas de dados da plataforma | Must |
| RN-04 | O custo operacional do módulo deve ser previsível e escalável de forma linear | Must |
| RN-05 | A metodologia de cálculo de cada métrica deve ser pública e auditável | Must |
| RN-06 | O módulo deve suportar monetização diferenciada por tier de assinatura | Should |
| RN-07 | O módulo deve permitir posicionamento comercial junto a usuários migrando de provedores descontinuados | Should |
| RN-08 | O módulo deve gerar engajamento recorrente (uso diário) | Should |
| RN-09 | A arquitetura deve permitir expansão para novas redes sem reescrita | Could |

---

## 7. Requisitos funcionais

| ID | Requisito | Prioridade |
|---|---|---|
| RF-01 | Coletar dados de taxa por bloco em Ethereum mainnet | Must |
| RF-02 | Persistir a série histórica com granularidade de bloco | Must |
| RF-03 | Calcular percentis de priority fee em janelas configuráveis | Must |
| RF-04 | Calcular índice de congestionamento por bloco e por janela | Must |
| RF-05 | Estimar custo em gwei e em USD para tipos de operação padronizados | Must |
| RF-06 | Calcular fee burn acumulado e emissão líquida de ETH | Must |
| RF-07 | Expor as métricas via endpoint de API | Must |
| RF-08 | Disponibilizar widget no construtor de dashboards | Must |
| RF-09 | Permitir criação de alerta por limiar de métrica | Should |
| RF-10 | Coletar e exibir dados de ao menos uma rede L2 | Should |
| RF-11 | Exibir comparativo de custo entre redes suportadas | Should |
| RF-12 | Exibir blob base fee (EIP-4844) | Should |
| RF-13 | Permitir sobreposição de métrica de gas com métrica de outro domínio no mesmo gráfico | Should |
| RF-14 | Exportar série histórica em CSV | Could |

---

## 8. Requisitos não funcionais

| ID | Requisito | Critério mensurável |
|---|---|---|
| RNF-01 | Latência de atualização | Dado disponível em ≤ 1 intervalo de bloco após confirmação |
| RNF-02 | Cobertura de coleta | ≥ 99,5% dos blocos do período, sem lacunas não registradas |
| RNF-03 | Resiliência de fornecedor | Fallback automático para provedor RPC secundário |
| RNF-04 | Envelope de custo | Consumo de infraestrutura dentro do teto do doc. 06 |
| RNF-05 | Tratamento de reorganização de cadeia | Bloco só é considerado final após N confirmações |
| RNF-06 | Rastreabilidade | Toda métrica versionada com fórmula, fonte e data de vigência |
| RNF-07 | Desempenho de consulta | Consulta de série de 30 dias responde em ≤ 2s |
| RNF-08 | Observabilidade | Métricas de consumo de RPC e de lacunas de coleta instrumentadas |
| RNF-09 | Documentação | Documentação de API e de metodologia entregue junto ao código |

---

## 9. Regras de negócio

| ID | Regra |
|---|---|
| RB-01 | Valores são armazenados em wei/gwei; a conversão para moeda fiduciária é feita na leitura, com cotação datada |
| RB-02 | Um bloco só entra na série consolidada após N confirmações; antes disso é marcado como provisório |
| RB-03 | Blocos ausentes são recuperados por backfill — nunca interpolados |
| RB-04 | Toda métrica derivada tem fórmula publicada; alterações de fórmula geram nova versão, sem reescrever a série anterior |
| RB-05 | Quebras de série por mudança de protocolo são anotadas na própria série, não removidas |
| RB-06 | Custo por tipo de operação usa consumo de gas de referência fixo e documentado, não estimativa dinâmica |

---

## 10. Critérios de sucesso

### 10.1 Quantitativos

| Indicador | Meta |
|---|---|
| Cobertura de blocos coletados | ≥ 99,5% |
| Latência do dado | ≤ 1 intervalo de bloco |
| Métricas proprietárias entregues | ≥ 4 |
| Custo mensal de infraestrutura no MVP | Dentro do teto do doc. 06 |
| Tempo de resposta de consulta histórica (30d) | ≤ 2s |
| Cobertura de documentação de metodologia | 100% das métricas |

### 10.2 Qualitativos

- Validação técnica pela engenharia da Alphractal, sem apontamento bloqueante de arquitetura
- Aceite do patrocinador quanto à aderência do módulo à proposta de valor da plataforma
- Avaliação de usabilidade positiva junto a ao menos um usuário do perfil institucional
- Código e documentação em estado que permita continuidade por outro time

---

## 11. Marcos e cronograma

| Sprint | Semanas | Entrega principal | Critério de saída |
|---|---|---|---|
| 1 | 1–2 | Análise de negócio, definição de escopo e arquitetura | Redes definidas, provedor RPC confirmado, ADR de arquitetura aprovado |
| 2 | 3–4 | Pipeline de coleta e persistência | Coleta contínua estável em Ethereum, cobertura ≥ 99% por 72h |
| 3 | 5–6 | Camada de métricas derivadas | 4+ métricas calculadas e validadas contra fonte independente |
| 4 | 7–8 | Exposição: API, widget e alertas | Métricas consumíveis por dashboard e endpoint |
| 5 | 9–10 | Integração cruzada, documentação e apresentação | Visualização cruzada entregue, documentação completa, demo validada |

---

## 12. Critérios de aceite

O projeto é considerado entregue quando:

1. O pipeline de coleta opera de forma contínua e autônoma, com cobertura verificada.
2. As métricas derivadas estão calculadas, validadas contra fonte externa independente e documentadas com fórmula.
3. As métricas são consumíveis por API e visíveis em dashboard.
4. Ao menos uma visualização cruza gas com dado de outro domínio da plataforma.
5. A documentação técnica e de metodologia está publicada junto ao repositório.
6. O relatório de custo de operação (real, medido, não estimado) foi entregue.
7. O parceiro validou tecnicamente a solução.

---

## 13. Dependências e riscos

**Dependências externas**
- Credenciais e limites do provedor RPC (bloqueante — ver PR-01)
- Definição de redes prioritárias pelo parceiro
- Acesso ao motor de dashboards e alertas da plataforma

**Riscos principais** — tratamento completo no documento 05:

| Risco | Severidade |
|---|---|
| R-01 Estouro de limite de requisições do provedor RPC | Crítico |
| R-04 Custo de infraestrutura acima do orçado | Alto |
| R-05 Mudança de protocolo alterando semântica das taxas | Alto |
| R-07 Percepção de comoditização pelo usuário | Alto |
| R-08 Inflação de escopo no horizonte de 10 semanas | Alto |

---

## 14. Documentos relacionados

- `00` Introdução do parceiro e SWOT
- `02` Business Model Canvas
- `03` Análise de mercado (Porter, PESTEL, Oceano Azul)
- `04` Benchmarking competitivo
- `05` Matriz de risco
- `06` Análise financeira e TCO
