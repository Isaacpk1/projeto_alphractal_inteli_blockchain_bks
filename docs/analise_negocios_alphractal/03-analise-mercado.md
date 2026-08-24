# Análise de Mercado — Alphractal | Módulo "Fees"
### 5 Forças de Porter · PESTEL · Matriz Oceano Azul

---

# Parte I — 5 Forças de Porter

**Delimitação do setor analisado:** plataformas de dados e analytics para mercados de criptoativos, com recorte no subsegmento de dados de custo de transação (gas).

## Síntese

| Força | Intensidade | Direção |
|---|---|---|
| 1. Rivalidade entre concorrentes | **Alta** | Estável, com consolidação em curso |
| 2. Ameaça de novos entrantes | **Média-alta** | Crescente no nicho, decrescente na plataforma completa |
| 3. Poder de barganha dos fornecedores | **Média-alta** | Crescente |
| 4. Poder de barganha dos compradores | **Alta** | Estável |
| 5. Ameaça de substitutos | **Alta** | Estável |

**Atratividade estrutural do setor: baixa** para quem vende dado bruto de gas; **média** para quem vende a camada de interpretação integrada.

---

## 1. Rivalidade entre concorrentes — ALTA

O segmento de analytics cripto tem players estabelecidos, bem capitalizados e com posicionamentos distintos: Glassnode e CryptoQuant dominam métricas on-chain pré-computadas para BTC e ETH; Nansen construiu vantagem em rotulagem de carteiras, com centenas de milhões de endereços classificados; Dune oferece acesso SQL bruto sobre mais de 100 blockchains; Messari combina research e dado estruturado.

Três dinâmicas agravam a rivalidade:

**Convergência de escopo.** Cada player está invadindo o território do outro — a Glassnode expandiu para derivativos e métricas de opções; a Nansen simplificou a estrutura de planos e lançou API por chamada. A diferenciação por categoria de dado está se dissolvendo.

**Guerra de preços por baixo.** A entrada é gratuita em praticamente todos os players. O que se disputa é a conversão, não o acesso.

**Consolidação em andamento.** O trimestre anterior à data desta análise registrou mais de vinte empresas de cripto reestruturando ou encerrando operações, em meio a condições de mercado adversas e custo operacional elevado — a Blocknative entre elas. Rivalidade alta em setor que encolhe é o pior dos cenários para margem.

> **Implicação para a Alphractal:** competir por cobertura de dado é competir onde os rivais são mais fortes e mais capitalizados. A defesa está na combinação — poucos players têm on-chain, derivativos, sentimento e macro sob o mesmo teto.

## 2. Ameaça de novos entrantes — MÉDIA-ALTA

Aqui é preciso separar dois níveis:

**No nicho de gas tracker: barreira baixíssima.** Os dados são públicos, os endpoints RPC são acessíveis e existe camada gratuita em todos os provedores. Um desenvolvedor competente entrega um tracker funcional em dias. É exatamente o motivo pelo qual existem dezenas deles.

**Na plataforma analítica integrada: barreira alta.** Construir acervo histórico de métricas, credibilidade metodológica e base de assinantes leva anos. A Alphractal levou de 2023 até aqui.

O risco real não é o entrante de garagem — é o **entrante lateral**: um provedor de infraestrutura (Alchemy, QuickNode) que decida subir na cadeia de valor e oferecer analytics sobre o dado que já coleta. Esse entrante tem o dado de graça, escala pronta e relacionamento com o cliente.

> **Implicação:** a barreira de entrada do módulo isolado é nula. A barreira do módulo *integrado ao acervo da plataforma* é considerável. Todo o esforço de diferenciação deve ir para a integração.

## 3. Poder de barganha dos fornecedores — MÉDIA-ALTA

Fornecedores relevantes: provedores de RPC/nós, exchanges (dados de derivativos e spot) e provedores de dado macro.

O modelo de cobrança dos provedores de RPC é baseado em unidades de computação: a Alchemy oferece 30 milhões de unidades mensais gratuitas e cobra a partir de US$ 0,45 por milhão de unidades no plano por uso, com tarifa decrescente acima de 300 milhões. Cada método consome uma quantidade diferente — uma chamada simples de número de bloco custa 10 unidades, uma chamada de contrato custa 26.

Isso significa que **o fornecedor controla diretamente a margem do módulo**: uma mudança na tabela de unidades ou nos limites de throughput altera o custo unitário sem qualquer negociação.

O poder é mitigável, mas não eliminável:
- Existem substitutos diretos (Infura, QuickNode, LogicNodes, nós próprios), o que limita abuso de preço
- Migrar entre provedores é operação conhecida no ecossistema, com custo de troca moderado
- Porém, a saída da Blocknative demonstrou o risco extremo: um fornecedor pode não apenas subir preço, mas **desaparecer com aviso de semanas**

> **Implicação:** arquitetura multi-provedor não é otimização — é requisito. Consta como RNF-03 no BRD.

## 4. Poder de barganha dos compradores — ALTA

- **Custo de troca baixo.** Assinaturas mensais, sem contrato de longo prazo, sem lock-in de dado. O usuário cancela em um clique.
- **Informação perfeita sobre preço.** Existem comparativos públicos de todos os players; o comprador sabe exatamente o que cada um cobra.
- **Alternativa gratuita sempre disponível.** Todo player tem tier gratuito, e o dado de gas especificamente é gratuito em vários lugares.
- **No institucional, concentração.** Poucos fundos grandes representam receita desproporcional e negociam condições.

O contrapeso é o **custo de troca cognitivo**: dashboards configurados, alertas criados e familiaridade com métricas proprietárias criam inércia. É a única alavanca real de retenção — e é exatamente o que alertas de gas reforçam.

## 5. Ameaça de substitutos — ALTA

Esta é a força mais crítica para o módulo especificamente.

| Substituto | Custo para o usuário | O que entrega | O que não entrega |
|---|---|---|---|
| Etherscan Gas Tracker | Gratuito | Gas atual, histórico recente, custo por dApp popular | Integração com dado de mercado |
| Owlracle e trackers similares | Gratuito / freemium | Gas multichain via API | Camada analítica |
| Dashboards públicos no Dune | Gratuito | Qualquer métrica que alguém já tenha escrito em SQL | Tempo real, curadoria |
| RPC próprio | Custo de infra | Controle total | Interpretação pronta |
| Interface da carteira | Gratuito | Estimativa no momento da transação | Qualquer análise |

**O dado de gas é estruturalmente comoditizado.** Não há como cobrar por ele. A prova mais eloquente é que a Blocknative — que tinha a melhor infraestrutura de estimativa do mercado, com previsão baseada em mempool e regressão quantílica — optou por **desligar o serviço em vez de vendê-lo**, sinalizando o quanto o mercado de dados e ferramentas cripto ficou competitivo e comprimido em margem.

> **Implicação central do projeto:** qualquer proposta de valor que dependa de vender o dado de gas está morta antes de nascer. O substituto gratuito é bom o suficiente. O que os substitutos **não** conseguem fazer — e não conseguirão, por não possuírem os dados — é cruzar taxa de rede com open interest, funding rate, fluxo de exchange e contexto macro. É esse o território defensável.

---

## Conclusão de Porter

O setor é estruturalmente hostil: cinco forças em intensidade média-alta ou alta, num mercado em consolidação. A leitura, porém, não é "não entrar" — é **entrar no lugar certo**.

Vender dado de gas: rivalidade alta, substitutos gratuitos, fornecedor com poder sobre a margem, comprador sem custo de troca. Combinação perdedora.

Vender interpretação de gas dentro de um acervo multi-domínio: rivalidade menor (poucos têm o acervo), substitutos ineficazes (não têm os outros dados), custo de troca maior (dashboards e alertas configurados). É onde as cinco forças pesam menos.

---

# Parte II — Análise PESTEL

## Político

- Regulamentação de prestadores de serviços de ativos virtuais no Brasil sob supervisão do Banco Central, decorrente do marco legal de 2022, empurra o setor para operação formalizada — o que favorece fornecedores de dado auditável.
- Movimentos regulatórios internacionais (MiCA na Europa, evolução da postura norte-americana) elevam a demanda por rastreabilidade e documentação metodológica.
- Entrada de consultorias tradicionais no espaço cripto — a própria absorção do time da Blocknative pela Deloitte é exemplo — sinaliza institucionalização e, junto com ela, exigências de governança de dados mais rígidas.

**Efeito líquido:** favorável a quem documenta metodologia. Reforça RN-05 e RNF-06 do BRD.

## Econômico

- Ciclicidade do mercado cripto afeta receita de assinatura e atividade on-chain **simultaneamente** — em retração, cai o número de assinantes e cai o próprio dado que o módulo mede. Risco correlacionado, não diversificado.
- Custos de infraestrutura são denominados em dólar; parte relevante da receita, em real. Exposição cambial estrutural para operação sediada no Brasil.
- Compressão de margem generalizada no setor de dados cripto, evidenciada pela onda de encerramentos.
- Juros altos reduzem apetite por ativos de risco e, por consequência, atividade de rede.

**Efeito líquido:** desfavorável no curto prazo. Reforça a exigência de custo variável controlado (RN-04).

## Social

- Brasil figura entre os mercados de maior adoção de cripto do mundo, com base de usuários grande e subatendida por ferramentas em português.
- Profissionalização do analista cripto: o perfil migrou de entusiasta para profissional com formação quantitativa, elevando a exigência por rigor metodológico.
- Cultura de comunidade e conteúdo aberto: research público funciona como aquisição de baixo custo, mas também comoditiza insight.

**Efeito líquido:** favorável. Sustenta a estratégia de conteúdo como canal e o diferencial de idioma.

## Tecnológico

Vetor de maior volatilidade para este módulo especificamente:

- **EIP-4844 e blobs** já alteraram a estrutura de custo das L2s, criando um segundo mercado de taxas (blob base fee) que praticamente nenhuma ferramenta analisa bem. Oportunidade direta.
- **Proliferação de L2s** torna a comparação de custo entre redes uma decisão recorrente e mal servida.
- **Abstração de conta** muda quem paga o gas e como, alterando a interpretação econômica da métrica.
- **Upgrades futuros de protocolo** podem redefinir a semântica das taxas e quebrar séries históricas — daí a regra RB-05.

**Efeito líquido:** ambivalente. Cria oportunidade analítica e risco de obsolescência ao mesmo tempo.

## Ecológico

- A migração do Ethereum para proof-of-stake removeu o consumo energético do centro do debate.
- Critérios ESG persistem em mandatos de fundos institucionais, especialmente europeus, mas com peso decrescente sobre redes PoS.

**Efeito líquido:** neutro. Baixa relevância para este módulo.

## Legal

- **LGPD:** dados on-chain são públicos, mas endereços de carteira podem, em certas interpretações e quando combinados com outras fontes, configurar dado pessoal. Métricas agregadas de gas não incorrem nesse risco — mas qualquer evolução do módulo para rastreamento de endereços individuais precisa de análise jurídica prévia.
- **Termos de uso de provedores:** limites de redistribuição de dados obtidos via RPC devem ser verificados antes de expor o dado em API paga. ⚠️
- **Propriedade de dado derivado:** métricas calculadas a partir de dado público são propriedade da Alphractal, mas a fórmula publicada é copiável — a proteção é de execução e marca, não jurídica.

**Efeito líquido:** neutro no escopo do MVP, com ponto de atenção na redistribuição via API.

---

# Parte III — Matriz Oceano Azul

## Matriz de Quatro Ações

### ELIMINAR
*O que o setor considera indispensável e deve ser abandonado*

- **Infraestrutura própria de monitoramento de mempool.** Foi o diferencial técnico da Blocknative e também o custo que ajudou a inviabilizá-la. Dados de bloco confirmado, mais baratos por ordens de grandeza, atendem ao caso de uso analítico — que não é o mesmo do caso de uso transacional.
- **Cobrança pelo dado bruto de gas.** Preço zero para o número; preço para a interpretação.
- **Corrida por número de redes suportadas.** Métrica de vaidade do setor que multiplica custo sem multiplicar valor.

### REDUZIR
*O que deve ficar bem abaixo do padrão do setor*

- **Granularidade indiscriminada.** Coleta por bloco em redes de bloco sub-segundo multiplica custo sem ganho analítico proporcional. Amostragem inteligente por rede.
- **Complexidade de configuração.** O Dune entrega poder absoluto ao custo de exigir SQL. O caminho oposto: métrica pronta, interpretável sem código.
- **Volume de indicadores como argumento comercial.** Contagem de métricas é a métrica de vaidade do setor de analytics.

### AUMENTAR
*O que deve ficar bem acima do padrão do setor*

- **Integração entre domínios de dado.** Gas sobreposto a open interest, funding e fluxo de exchange no mesmo gráfico. É o que nenhum tracker consegue fazer.
- **Explicabilidade.** Fórmula, fonte e versão publicadas para cada métrica — em um setor onde muita métrica proprietária é caixa-preta.
- **Profundidade histórica com integridade.** Série contínua, com quebras anotadas em vez de disfarçadas.
- **Contexto econômico da taxa.** Não "quanto custa", mas "o que esse custo significa".

### CRIAR
*O que o setor nunca ofereceu*

- **Taxa de rede como classe de indicador de mercado.** Congestionamento como proxy de demanda real; queima via EIP-1559 como variável de oferta de ETH; diferencial de custo L1 vs L2 como sinal de rotação de capital entre ecossistemas.
- **Índice proprietário de congestionamento normalizado**, comparável entre redes e ao longo do tempo.
- **Custo efetivo por tipo de operação econômica**, e não por unidade de gas — quanto custa fazer um swap, não quanto vale um gwei.
- **Alerta de custo acionável** integrado ao mesmo motor que dispara alertas de preço e de fluxo.
- **Análise do mercado de blobs (EIP-4844)** como camada econômica das L2s.

---

## Curva de Valor

Atributos avaliados em escala de 1 (baixo) a 5 (alto):

| Atributo | Etherscan Gas Tracker | Owlracle | Dune | Glassnode | **Alphractal + Fees** |
|---|:---:|:---:|:---:|:---:|:---:|
| Dado de gas em tempo real | 5 | 5 | 2 | 1 | **4** |
| Cobertura de redes | 3 | 5 | 5 | 2 | **2** |
| Profundidade histórica de taxas | 3 | 2 | 4 | 1 | **4** |
| Interpretação analítica da taxa | 1 | 1 | 2 | 1 | **5** |
| Integração com dados de mercado | 1 | 1 | 3 | 4 | **5** |
| Facilidade de uso (sem código) | 5 | 4 | 1 | 4 | **5** |
| Explicabilidade metodológica | 2 | 2 | 5 | 3 | **5** |
| Alertas configuráveis | 1 | 2 | 2 | 3 | **5** |
| Acessibilidade de preço | 5 | 5 | 4 | 2 | **4** |

**Leitura da curva.** A Alphractal deliberadamente **perde** em cobertura de redes e empata em tempo real — não são batalhas vencíveis nem valiosas. A separação acontece em quatro atributos onde os concorrentes pontuam de 1 a 3: interpretação analítica, integração com dados de mercado, explicabilidade e alertas. É uma curva de valor divergente, não uma versão melhorada da curva do setor — que é a condição para um oceano azul.

---

## Declaração de posicionamento

> **Para** analistas e mesas que operam on-chain,
> **que precisam** entender não apenas quanto custa transacionar, mas o que o custo de rede revela sobre o mercado,
> **o módulo Fees da Alphractal** é uma camada de inteligência de taxas
> **que** transforma gas em indicador de mercado cruzável com derivativos, fluxo e macro.
> **Diferentemente de** Etherscan, Owlracle e demais trackers, que entregam o número isolado,
> **nosso produto** entrega o número dentro do contexto que o torna acionável.

---

## Ressalva metodológica

A análise Oceano Azul assume que existe demanda não atendida por interpretação econômica de taxas de rede. **Essa é uma hipótese, não um fato verificado** (H2 e H4 do documento 02). O encerramento da Blocknative admite duas leituras opostas: ou o mercado de dados de gas não se sustenta comercialmente, ou o modelo específico dela — infraestrutura de mempool cara vendendo dado comoditizado — é que não se sustentava.

Este projeto aposta na segunda leitura. A aposta é razoável, mas deve ser tratada como aposta, e testada antes de escalar investimento.
