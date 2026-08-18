[← Índice](./README.md)

# 05 — Backlog de Diferenciais (D)

> Itens **além do escopo mínimo do TAP**, priorizados por relação valor/esforço.
> Nada aqui deve ser iniciado antes de todos os RF **[M]** estarem fechados.

## Critério de seleção

O TAP define o problema como a transição de *"dados puramente informativos"* para *"indicadores operacionais acionáveis"*. Portanto, **um diferencial só entra neste backlog se responder à pergunta "o que eu faço agora?"** — não à pergunta "qual é o número?". Mostrar mais um número não agrega: o Etherscan Gas Tracker já faz isso de graça.

**Esforço** em dias-desenvolvedor (1 dev, considerando o backend já pronto).

> 📌 **A stack ClickHouse barateou este backlog.** `quantile()` nativo e *materialized views* tornam **D-02** (percentil) e **D-04** (heatmap) muito mais simples do que seriam em SQLite — eram os dois itens mais caros da Onda 1 e 2. Em contrapartida, ambos passam a depender do caminho frio estar de pé (ver [09 §4](./09-arquitetura-e-stack.md)).

---

## Onda 1 — Alto valor, baixo esforço (recomendados)

### D-01 · Calculadora de custo da operação (não da transação) — 1,5 dia · impacto alto

Gestor institucional não executa um swap: executa uma ordem de US$ 2 M fatiada em 8 swaps. O usuário informa **notional** e **número de transações**, e o painel mostra o custo total da operação em USD **e em basis points sobre o notional**.

Basis points são a linguagem de mesa de operações — traduzir gas para bps é o que transforma o painel em ferramenta de decisão de execução.

**Critérios de aceite**

- [ ] Campos de entrada: valor da operação (USD), tipo de transação e quantidade de transações.
- [ ] Saída: custo total em ETH, em USD e em **bps sobre o notional** (`custo_usd / notional × 10.000`).
- [ ] Recalcula automaticamente a cada novo bloco recebido, sem perder o que o usuário digitou.
- [ ] Cobre as três faixas de velocidade simultaneamente (Lento / Padrão / Rápido) lado a lado.
- [ ] Notional zero ou vazio não quebra a UI nem gera divisão por zero.

### D-02 · Percentil histórico do gas atual — 1 dia · impacto alto

Exibir **"24 gwei = percentil 18 dos últimos 30 dias"** ao lado do indicador da RN-04. Dá contexto de **nível** onde a RN-04 só dá **variação**, é imediatamente interpretável e justifica o investimento no banco de dados.

> ⚠️ **Complemento, não substituto da RN-04.** Uma versão anterior deste documento propunha trocar uma pela outra — isso era um erro. Elas respondem a perguntas diferentes, e um indicador puramente histórico se aproximaria justamente das *"médias históricas estáticas"* que o TAP aponta como o problema a resolver. O percentil contextualiza; a RN-04 detecta o movimento.

**Critérios de aceite**

- [ ] Percentil calculado sobre a tabela `fee_stats_hourly`, com janelas de 7 e 30 dias.
- [ ] Frase interpretativa gerada automaticamente (ex.: "mais barato que 82% da última semana").
- [ ] Degrada com elegância quando há menos de 24 h de histórico acumulado (exibe a janela disponível e sinaliza isso).
- [ ] Recalculado no máximo a cada bloco; consulta não bloqueia o stream (RNF-25).

### D-03 · Custo de oportunidade da espera — 1 dia · impacto alto

*"Executando agora: US$ 340 · Aguardando o próximo bloco: US$ 298 (−12%)"*.

Não é previsão estatística: a projeção da base fee do próximo bloco é **determinística** pela regra do EIP-1559 (RN-05). Barato de implementar e é exatamente o "acionável" que o TAP pede.

**Critérios de aceite**

- [ ] Usa a projeção da RN-05 (`gasUsed` vs `gasLimit/2`, teto de ±12,5%).
- [ ] Exibe custo atual, custo projetado e a diferença em USD e em %.
- [ ] Deixa explícito que o horizonte é **1 bloco (~12 s)** — sem sugerir previsão de longo prazo.
- [ ] Teste unitário cobrindo os três casos da RN-05 (bloco cheio, vazio, exatamente na metade).

### D-07 · Latência exibida no painel — 0,5 dia · impacto médio-alto

Mostrar *"dado de 1,2 s atrás"*. Parece detalhe, mas é o que **prova** que o produto é tempo real de verdade — o argumento central do projeto. O dado já existe no schema (`ingested_at`).

**Critérios de aceite**

- [ ] Latência = `agora − block_timestamp`, atualizada a cada segundo no cliente.
- [ ] Muda de estado visual ao ultrapassar o limiar da RN-07 (60 s → "dados desatualizados").
- [ ] Não depende de sincronia de relógio entre cliente e servidor (calcular a defasagem no backend e enviar no evento).

---

## Onda 2 — Alto impacto, esforço médio

### D-04 · Heatmap dia da semana × hora (UTC) — 2 dias · impacto alto

Matriz 7 × 24 com a base fee média histórica. Traduz direto em *"execute terça às 06 h UTC e pague 40% menos"*. É a feature que um gestor tira print e manda no grupo do fundo.

**Critérios de aceite**

- [ ] Alimentado por `fee_stats_hourly`; nenhuma agregação pesada em tempo de request.
- [ ] Escala de cor sequencial acessível, com valor numérico legível no tooltip (RNF-19).
- [ ] Destaca a célula correspondente ao momento atual.
- [ ] Indica claramente o volume de dados por trás (ex.: "baseado em 12 dias de coleta") — sem isso o heatmap mente nas primeiras semanas.
- [ ] **Dependência:** requer ≥ 7 dias de dados acumulados, ou seed histórico via RPC/API externa.

### D-05 · Comparativo L1 vs L2 — 2,5 dias · impacto alto

O mesmo swap na Ethereum, Base, Arbitrum e Optimism, lado a lado. Para um usuário institucional isso é decisão de roteamento de capital. O TAP já cita escalar para L2 como benefício esperado — entregar isso antecipa a visão do parceiro.

**Critérios de aceite**

- [ ] Uma conexão RPC por rede, reutilizando a mesma camada de *provider* (RNF-14) sem duplicar lógica.
- [ ] Custo normalizado para o mesmo tipo de transação nas quatro redes.
- [ ] Exibe a economia relativa à Mainnet em % e em USD.
- [ ] Falha de uma rede não afeta a exibição das demais.

### D-06 · Detecção e atribuição de picos — 3 dias · impacto muito alto (demo)

Quando a base fee dispara acima de um limiar, marcar o evento no gráfico e identificar **qual contrato mais consumiu gas** naquele bloco → *"pico causado por mint no contrato 0xABC…"*.

Isso é inteligência de mercado — literalmente o negócio da Alphractal. É o item de maior efeito no Demo Day e o que mais diferencia o projeto de um gas tracker genérico.

**Critérios de aceite**

- [ ] Detecção: variação da base fee acima de X% em relação à média móvel, com limiar configurável.
- [ ] Agrega o gas consumido por endereço de destino nas transações do bloco.
- [ ] Resolve o nome do contrato quando possível (fonte de labels a definir); *fallback* para endereço abreviado.
- [ ] Marcador clicável no gráfico com o detalhe do evento.
- [ ] **Risco:** exige `eth_getBlockByNumber` com transações completas — verificar impacto no consumo do plano RPC (RNF-05) antes de commitar.

---

## Onda 3 — Se sobrar tempo

| ID | Ideia | Esforço | Por que vale |
|---|---|---|---|
| D-08 | **Blob fees (EIP-4844)** — custo de dados dos rollups | 1,5 dia | Poucos dashboards cobrem bem, e é exatamente o público que opera L2 |
| D-09 | **Modo replay** de um período gravado | 1 dia | Salva a demo se a rede estiver calma em 05/10; serve de fixture de teste |
| D-10 | **Sobreposição gas × volatilidade do ETH** | 2 dias | Correlação de dados é o terreno natural da Alphractal |
| D-11 | **Expor o stream como API/webhook pública** | 1 dia | Aumenta a chance de o parceiro absorver o código de verdade |
| D-12 | **Alertas por e-mail/Telegram** no limiar do RF-30 | 1,5 dia | Extensão natural do alerta visual — confirmar interesse (dúvida nº 9) |

---

## Explicitamente fora do backlog

**Previsão de gas com machine learning.** Soa impressionante na proposta e é uma armadilha em 4 semanas: dados insuficientes, validação inviável no prazo, e errar ao vivo na apresentação desmoraliza o restante do trabalho. A regra do EIP-1559 (RN-05) já entrega previsão de 1 bloco com precisão matemática — usar isso e ser honesto sobre o horizonte é tecnicamente mais defensável do que um modelo não validado.

**Execução ou simulação de envio de transações.** Vedado pelo TAP (RN-12), não é decisão técnica.

---

## Sequenciamento sugerido

| Momento | Foco |
|---|---|
| Semanas 1–3 | Todos os RF **[M]** e **[S]** — nada deste backlog |
| Semana 3 (fim) | D-07 e D-03 (baratos, alto retorno na narrativa) |
| Semana 4 | D-01 e D-02 se o MVP estiver estável; D-09 como seguro para a demo |
| Pós-projeto | Onda 2 e 3, como proposta de continuidade ao parceiro |
