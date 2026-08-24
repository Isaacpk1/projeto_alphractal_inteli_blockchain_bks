# Análise Financeira e TCO — Módulo "Fees"

---

## 1. Escopo e limitações desta análise

Esta análise dimensiona o **custo total de propriedade** do módulo e estabelece o ponto de equilíbrio em número de assinantes. Três ressalvas importantes:

1. **O lado da receita é hipótese, não projeção.** Não há dados de conversão ou retenção para modelar ganho com precisão. A análise trabalha com *break-even* — quantos assinantes o módulo precisa gerar ou reter para se pagar — que é uma pergunta respondível sem inventar receita.
2. **Os valores unitários são públicos; os volumes são estimados.** Preços de provedores foram levantados em fontes oficiais. Os consumos por método marcados com ⚠️ são premissas a validar contra a tabela oficial de unidades de computação.
3. **Custo de desenvolvimento é acadêmico.** O valor de referência serve apenas para dimensionar o investimento equivalente, não corresponde a desembolso real.

---

## 2. Premissas de volumetria

| Parâmetro | Valor | Origem |
|---|---|---|
| Intervalo de bloco — Ethereum | 12 s → **7.200 blocos/dia** | Parâmetro de protocolo |
| Intervalo de bloco — Base | ~2 s → **43.200 blocos/dia** | ⚠️ Verificar |
| Intervalo de bloco — Arbitrum | ~0,25 s → **345.600 blocos/dia** | ⚠️ Verificar |
| Custo de evento WebSocket | ~40 unidades por evento de ~1.000 bytes | Documentação Alchemy |
| Custo de chamada RPC de leitura de bloco | ~25 unidades ⚠️ | Estimativa — chamada simples de número de bloco custa 10 unidades e chamada de contrato custa 26 |
| Camada gratuita Alchemy | 30 milhões de unidades/mês | Tabela pública |
| Preço por uso | US$ 0,45 por milhão até 300 M; US$ 0,40 acima | Tabela pública |
| Mês de referência | 30 dias | Convenção |

---

## 3. Cenários de custo de coleta

### Cenário A — MVP: Ethereum, WebSocket + complemento por bloco

| Componente | Cálculo | Unidades/mês |
|---|---|---|
| Assinatura de novos blocos (WebSocket) | 7.200/dia × 40 × 30 | 8,64 M |
| Chamada complementar de histórico de taxas | 7.200/dia × 25 × 30 | 5,40 M |
| **Total** | | **14,04 M** |

**Cabe integralmente na camada gratuita (30 M).**

### 💰 Custo de RPC: **US$ 0,00/mês**

---

### Cenário B — Contraexemplo: polling em vez de WebSocket

Para não perder blocos com polling, é preciso consultar a cada ~3 s:

| Componente | Cálculo | Unidades/mês |
|---|---|---|
| Consulta a cada 3 s, 2 chamadas | 28.800/dia × 2 × 25 × 30 | 43,20 M |

### 💰 Custo de RPC: **≈ US$ 19,44/mês**

> **Conclusão que vale mais que o número:** o mesmo resultado analítico custa **três vezes mais** com polling. A escolha de WebSocket não é preferência de arquitetura — é decisão econômica, e é o que mantém o MVP dentro da camada gratuita. Isso justifica o RNF-01 e mitiga diretamente o risco R-01.

---

### Cenário C — Expansão ingênua: 3 redes, coleta por bloco

| Rede | Blocos/dia | Unidades/dia (65 por bloco) |
|---|---:|---:|
| Ethereum | 7.200 | 468.000 |
| Base | 43.200 | 2.808.000 |
| Arbitrum | 345.600 | 22.464.000 |
| **Total** | **396.000** | **25,74 M** |

Consumo mensal: **772,2 M unidades**

| Faixa | Volume | Preço | Subtotal |
|---|---:|---:|---:|
| Até 300 M | 300 M | US$ 0,45/M | US$ 135,00 |
| Acima de 300 M | 472,2 M | US$ 0,40/M | US$ 188,88 |

### 💰 Custo de RPC: **≈ US$ 323,88/mês** (US$ 3.886/ano)

---

### Cenário D — Expansão inteligente: 3 redes, amostragem uniforme de 12 s

| Componente | Cálculo | Unidades/mês |
|---|---|---|
| 3 redes, 7.200 amostras/dia cada | 21.600/dia × 65 × 30 | 42,12 M |

### 💰 Custo de RPC: **≈ US$ 18,95/mês** (US$ 227/ano)

---

### 📊 Comparação dos cenários

| Cenário | Redes | Granularidade | Custo/mês | Custo/ano | Índice |
|---|:---:|---|---:|---:|:---:|
| **A** — MVP | 1 | Por bloco (12 s) | US$ 0 | US$ 0 | — |
| **B** — Polling | 1 | Por bloco (12 s) | US$ 19,44 | US$ 233 | 1,0× |
| **D** — Amostrado | 3 | 12 s | US$ 18,95 | US$ 227 | 1,0× |
| **C** — Por bloco | 3 | Nativa da rede | US$ 323,88 | US$ 3.886 | **17,1×** |

**A descoberta central da análise financeira está nesta tabela.** Cobrir três redes com amostragem de 12 segundos custa praticamente o mesmo que cobrir uma rede com polling ineficiente. Cobrir as mesmas três redes na granularidade nativa custa **17 vezes mais** — e o ganho analítico é marginal, porque nenhuma decisão de investimento se toma na janela de 250 milissegundos.

> **Recomendação:** adotar A no MVP, evoluir para D na expansão, **nunca** C. Formalizar como regra de arquitetura: granularidade de coleta é decisão econômica, não técnica.

---

## 4. Custo de armazenamento

Registro por bloco: número, timestamp, base fee, gas usado, gas limite, blob base fee e percentis — cerca de **150 bytes** antes de compressão.

| Configuração | Registros/mês | Volume/mês | Volume/ano |
|---|---:|---:|---:|
| Cenário A (Ethereum) | 216 mil | ~32 MB | ~390 MB |
| Cenário D (3 redes, 12 s) | 648 mil | ~97 MB | ~1,2 GB |
| Cenário C (3 redes, nativa) | 11,9 M | ~1,8 GB | ~21 GB |

**Custo de armazenamento é irrelevante em todos os cenários.** Mesmo o pior caso cabe na camada gratuita da maioria dos bancos de série temporal gerenciados. O custo de dado neste projeto está inteiramente na *ingestão*, não na *retenção*.

---

## 5. Backfill histórico (custo único)

| Profundidade | Blocos | Unidades | Custo único |
|---|---:|---:|---:|
| 6 meses de Ethereum | 1,31 M | 32,8 M | ~US$ 15 |
| 1 ano de Ethereum | 2,63 M | 65,7 M | ~US$ 30 |
| 2 anos de Ethereum | 5,26 M | 131,4 M | **~US$ 59** |

> Executar o backfill em janela de baixa demanda para não competir com a cota da coleta corrente. Dois anos de histórico por menos de sessenta dólares é excelente relação custo-benefício — profundidade histórica é justamente um dos atributos onde a curva de valor prevê diferenciação.

---

## 6. Custo de hospedagem e serviços

| Item | Se rodar na infra existente | Se provisionado à parte |
|---|---:|---:|
| Serviço coletor (container) | ~US$ 0 marginal | US$ 5–20/mês |
| Banco de série temporal | ~US$ 0 marginal | US$ 0–50/mês ⚠️ |
| Cache | ~US$ 0 marginal | US$ 0–15/mês |
| Observabilidade | ~US$ 0 marginal | US$ 0–10/mês |
| **Total** | **~US$ 0** | **US$ 5–95/mês** |

⚠️ A Alphractal já opera pipelines de ingestão, API e motor de alertas. A premissa mais provável é custo marginal próximo de zero — **confirmar com a engenharia do parceiro.**

---

## 7. TCO consolidado — ano 1

| Linha | Cenário conservador (A, infra existente) | Cenário de expansão (D, infra própria) |
|---|---:|---:|
| RPC — coleta | US$ 0 | US$ 227 |
| RPC — backfill (único) | US$ 59 | US$ 59 |
| Armazenamento | US$ 0 | US$ 0–120 |
| Hospedagem e serviços | US$ 0 | US$ 60–1.140 |
| **TCO ano 1** | **≈ US$ 59** | **≈ US$ 346 – 1.546** |
| **TCO recorrente (ano 2+)** | **≈ US$ 0** | **≈ US$ 287 – 1.487** |

**Ordem de grandeza:** o custo operacional do módulo, no desenho recomendado, é de dezenas a poucas centenas de dólares por ano. Isso é substancialmente menor do que a intuição sugere e reposiciona a decisão: **o gargalo do projeto não é custo de infraestrutura, é custo de engenharia e risco de posicionamento.**

---

## 8. Custo de desenvolvimento (referencial)

Valor não desembolsado — o desenvolvimento é acadêmico. Serve para dimensionar o investimento equivalente:

| Parâmetro | Valor |
|---|---|
| Equipe | 4–5 pessoas ⚠️ |
| Duração | 10 semanas |
| Dedicação estimada | ~20 h/pessoa/semana |
| Esforço total | ~800–1.000 horas |
| Valor equivalente de mercado | Aplicar a taxa-hora de referência do parceiro |

**Ponto relevante para a discussão de valor:** o investimento equivalente em engenharia supera o custo de infraestrutura em **duas a três ordens de grandeza**. Qualquer decisão de otimização deve priorizar tempo de desenvolvimento sobre economia de infraestrutura — cortar escopo para caber no prazo vale mais que cortar granularidade para economizar dólares.

---

## 9. Ponto de equilíbrio

Assumindo assinatura de referência de **US$ 30/mês** ⚠️ *(reconfirmar a tabela vigente)*:

| Cenário de custo | Custo mensal | Assinantes adicionais para equilíbrio |
|---|---:|:---:|
| A — MVP na infra existente | US$ 0 | **0** |
| D — 3 redes, infra existente | US$ 19 | **1** |
| D — 3 redes, infra própria | US$ 115 | **4** |
| C — 3 redes por bloco, infra própria | US$ 420 | **14** |

O equilíbrio também pode vir por **retenção**: evitar 1 cancelamento por mês tem o mesmo efeito financeiro que conquistar 1 assinante. Como alertas de gas são a feature de maior frequência de retorno disponível na plataforma, a via de retenção é provavelmente a mais realista.

> **Interpretação:** no desenho recomendado, o módulo se paga com **um único assinante adicional ou um único cancelamento evitado por mês**. Esse é um patamar de risco financeiro desprezível.

---

## 10. Análise de sensibilidade

Variação do custo mensal em função das duas variáveis dominantes:

| | 1 rede | 3 redes | 6 redes |
|---|---:|---:|---:|
| **Amostragem 60 s** | US$ 0 | US$ 4 | US$ 8 |
| **Amostragem 12 s** | US$ 0 | US$ 19 | US$ 38 |
| **Por bloco (nativa)** | US$ 0 | US$ 324 | US$ 650+ |

**Duas leituras:**

1. **Número de redes escala o custo de forma linear e benigna.** Dobrar redes com amostragem fixa dobra um número pequeno.
2. **Granularidade escala de forma explosiva e desigual.** O salto de amostragem de 12 s para granularidade nativa multiplica o custo por 17 — e o multiplicador depende inteiramente de *quais* redes, porque redes de bloco sub-segundo dominam a conta sozinhas.

**Variável de controle:** granularidade, não cobertura. Deve ser parâmetro configurável por rede, com teto de gasto instrumentado (mitigação de R-04).

---

## 11. Ganhos esperados

### Quantificáveis com dados que ainda não temos

| Vetor | Mecanismo | Como medir |
|---|---|---|
| Redução de churn | Alertas de gas elevam frequência de retorno | Coorte com e sem alerta ativo |
| Conversão para tier pago | Dado em tempo real como gatilho de upgrade | Teste A/B de gating do módulo |
| Consumo de API | Endpoint de fees monetizado por crédito | Telemetria de consumo por endpoint |
| Captura de base órfã | Usuários migrando de provedor descontinuado | Origem de cadastro após campanha |

### Não quantificáveis, mas relevantes

- **Coerência da proposta de valor.** Fechar a lacuna mais visível da camada on-chain repara a promessa de consolidação — difícil de medir, central para o produto.
- **Camada de streaming reaproveitável.** O maior ativo técnico do projeto talvez não seja o módulo, e sim a infraestrutura de tempo real que ele obriga a construir, disponível depois para qualquer outra métrica.
- **Posicionamento em espaço desocupado.** Nenhum concorrente cruza gas com dados de mercado hoje.

---

## 12. Conclusões e recomendações

**1. O custo de infraestrutura não é obstáculo.** O MVP roda dentro da camada gratuita. O TCO do primeiro ano no desenho recomendado é de aproximadamente sessenta dólares.

**2. A decisão de arquitetura mais cara é a de granularidade.** Coleta por bloco em redes de bloco rápido multiplica o custo por 17 sem ganho analítico proporcional. Deve ser parâmetro configurável, com teto de gasto.

**3. WebSocket em vez de polling é decisão financeira.** Reduz o consumo em cerca de 3× e é o que mantém o MVP gratuito.

**4. O ponto de equilíbrio é trivialmente baixo.** Um assinante ou um cancelamento evitado por mês. O risco financeiro do projeto é desprezível.

**5. O risco real é de posicionamento, não de custo.** Como registrado em R-07: entregar um produto tecnicamente correto que o usuário percebe como redundante frente a alternativas gratuitas. Nenhuma economia de infraestrutura compensa esse desfecho.

---

## 13. Itens a validar antes de fechar a análise ⚠️

- [ ] Provedor de RPC contratado pela Alphractal e plano vigente
- [ ] Cota de unidades de computação já comprometida com outros módulos
- [ ] Custo de unidades por método na tabela oficial do provedor
- [ ] Disponibilidade de banco de série temporal na infraestrutura existente
- [ ] Tabela de preços de assinatura vigente (entra no cálculo de equilíbrio)
- [ ] Intervalo de bloco real das L2s candidatas
- [ ] Restrições contratuais de redistribuição de dados via API paga
