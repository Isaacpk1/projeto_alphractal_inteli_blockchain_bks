[← Índice](./README.md)

# 03 — Regras de Negócio (RN)

> Todas as regras abaixo vivem na **camada de serviço do backend** (RN-09). O frontend apenas formata e exibe.

---

## Cálculo de custo

### RN-01 — Custo da transação (EIP-1559)

```
custo_wei = (baseFeePerGas + priorityFee) × gasLimit
```

O `baseFee` é definido pelo protocolo e **queimado**; a *priority fee* (gorjeta ao validador) é a única variável que o usuário controla e é o que define a velocidade de inclusão.

### RN-02 — Faixas de velocidade

Derivadas dos percentis de *priority fee* retornados por `eth_feeHistory` sobre os últimos **`N_fee` = 20** blocos:

| Faixa | Percentil | Expectativa |
|---|---|---|
| Lento | p10 | inclusão em vários blocos |
| Padrão | p50 | próximos blocos |
| Rápido | p90 | próximo bloco |

**Janela:** `N_fee = 20 blocos` (≈4 min) — responde rápido a mudanças sem oscilar a cada bloco isolado.

**Consolidação da janela:** o `eth_feeHistory` devolve os três percentis *por bloco*. A faixa exibida é a **mediana** de cada coluna ao longo dos 20 blocos, não a média: um único bloco com gorjeta atípica (bot de MEV, liquidação) desloca a média e faria o painel sugerir um preço que ninguém precisa pagar.

*Definido pelo time: o parceiro deu liberdade explícita nas faixas ("sejam livres", 18/08 — [doc 10](./10-registro-respostas-parceiro.md)). Percentis e janela são **configuráveis** (`Fees:Percentiles` e `Fees:FeeWindowBlocks`), porque o parceiro sinalizou que "depois a gente manda alguma coisa".*

> **Por que p10 e não p25.** Versões anteriores deste documento diziam p25. O
> schema do ClickHouse (`priority_fee_p10`), o contrato do ETL e o backfill foram
> escritos em p10, e a divergência só apareceu quando a API foi implementada.
> Fixamos em **p10** porque o custo de mudar é assimétrico: alinhar o documento é
> uma linha, alinhar o schema exige recriar o volume do banco. A escolha é do time
> e revisável — se a Alphractal pedir p25, trocar é editar `Fees:Percentiles` e a
> coluna correspondente. O trade-off real: p10 mostra o piso do mercado e separa
> bem as três faixas no painel, mas "Lento" pode demorar dezenas de minutos em
> congestionamento; p25 é mais fiel ao rótulo e mais próximo do p50 em rede calma.

### RN-03 — Conversão para USD

```
custo_usd = custo_eth × preço_ETH_USD
```

A cotação é atualizada no máximo a cada **60 s**. Se estiver defasada há mais de **5 min**, o valor em USD deve ser exibido como desatualizado.

### RN-17 — Total de taxas efetivamente pagas

O agregado **Total Fees** não usa o percentil mediano da RN-02. Ele soma a taxa
efetiva de cada transação a partir dos recibos:

```
total_fee_wei(bloco) = Σ (receipt.gasUsed × receipt.effectiveGasPrice)
```

`burned_wei` contém somente a base fee queimada; `total_fee_wei` contém base fee
mais gorjeta. A mediana de `priorityFee` é apropriada para recomendar uma faixa
de velocidade, mas não representa a média ponderada das gorjetas pagas e
subestima o total quando contratos, MEV e liquidações dominam o bloco. Decisão e
validação: [ADR-004](../adr/004-total-de-taxas-vem-do-recibo.md).

### RN-06 — Unidades e precisão

- Exibição: gwei com 2 casas · USD com 2 casas · ETH com até 6 casas.
- **Toda aritmética interna usa inteiros em wei**, nunca ponto flutuante. A conversão para decimal acontece somente na formatação final.
  - **.NET:** `System.Numerics.BigInteger` (a Nethereum já opera com ele de ponta a ponta).
  - **ClickHouse:** `UInt64` / `UInt256` nativos. Valores monetários em `Decimal`, nunca `Float64`.
  - **React:** recebe valores **já formatados pelo backend** (RN-09) — o front não faz aritmética de wei, o que contorna o limite de 2⁵³ do `number` do JavaScript.

> A versão anterior desta especificação exigia guardar wei como `TEXT` no banco, por limitação do SQLite. **Regra revogada:** o ClickHouse tem tipos inteiros largos nativos.

### RN-11 — Gas limits de referência

Constantes de configuração, documentadas e ajustáveis sem alterar código de negócio:

| Tipo de transação | Gas limit de referência |
|---|---|
| Transferência de ETH | 21.000 |
| Transferência ERC-20 | ~65.000 |
| Aprovação (`approve`) | ~46.000 |
| Swap em DEX | ~150.000 |
| Mint de NFT | ~85.000 |

*Definido pelo time, com liberdade concedida pelo parceiro em 18/08 ([doc 10](./10-registro-respostas-parceiro.md)). Valores em arquivo de configuração — ajustáveis quando a Alphractal enviar os tipos de transação que o usuário institucional deles mais executa.*

---

## Estado da rede

### RN-04 — Índice de congestionamento

Compara a `baseFee` atual com a média móvel dos últimos **`N_cong = 100` blocos** (≈20 min):

| Faixa | Relação com a média | Rótulo |
|---|---|---|
| < 70% | bem abaixo | Baixo |
| 70–130% | dentro do normal | Normal |
| 130–200% | acima | Alto |
| > 200% | muito acima | Extremo |

**Procedência:** o termo *"saúde da rede"* vem literalmente do TAP — *"uma métrica de 'saúde' atual da rede"* (seção 2, Problema). O **método de cálculo e as faixas acima não vêm do TAP**: são definição do time. Em 18/08/2026 o parceiro concedeu liberdade explícita sobre indicadores (*"faz aê, sejam livres"* — [doc 10](./10-registro-respostas-parceiro.md)), encerrando a dúvida nº 6. As faixas ficam em configuração, não em código.

**Limitação conhecida desta regra.** Por comparar com uma média móvel curta, ela mede **variação**, não **nível**. Num período sustentado de taxas altas, a média móvel acompanha a subida e o indicador volta a marcar "Normal" — mesmo com o gas historicamente caro. É um ponto cego real, e o diferencial **D-02** (percentil histórico) existe para cobri-lo. Os dois são **complementares, não substitutos**: esta regra responde *"está subindo agora?"*; o percentil responde *"está caro em termos históricos?"*.

### RN-05 — Projeção da base fee do próximo bloco

Regra determinística do protocolo, não previsão estatística. O valor é **exato**, não uma estimativa.

Seja `alvo = gasLimit / 2`:

```
gasUsed = alvo  →  próxima = baseFee
gasUsed > alvo  →  próxima = baseFee + max(baseFee × (gasUsed − alvo) / alvo / 8, 1 wei)
gasUsed < alvo  →  próxima = baseFee − baseFee × (alvo − gasUsed) / alvo / 8
```

> **Atenção — versões anteriores deste documento descreviam a regra como "sobe no
> máximo +12,5%", e isso induz a implementação errada.** Os 12,5% são o **limite**,
> atingido apenas quando o bloco usa *todo* o gas disponível; a variação real é
> **proporcional** ao quanto o gas usado se afasta do alvo. Um bloco a 3/4 do
> limite sobe 6,25%, não 12,5%. Implementar ao pé da letra da redação antiga
> erraria a projeção em todo bloco que não fosse completamente cheio ou
> completamente vazio — ou seja, em praticamente todos.

O incremento mínimo de 1 wei na subida é parte do protocolo: sem ele, base fees
muito baixas arredondariam o delta para zero e nunca reagiriam a congestionamento.

Toda a aritmética é feita em inteiros (`BigInteger`), como manda a RN-06 — a
divisão por 8 é inteira, exatamente como na especificação do EIP-1559.

### RN-07 — Dado obsoleto (*stale*)

Se não chegar bloco novo em mais de **60 s** (≈5 blocos), o painel deve sair do estado "Ao vivo" e sinalizar dados desatualizados. Exibir número velho como se fosse atual é pior do que exibir erro — é justamente o problema que o projeto veio resolver.

### RN-08 / RN-16 — Reorganização de cadeia (*reorg*)

Se chegar um bloco com número **menor ou igual** ao último processado, ele **substitui** o anterior — nunca é duplicado nem mantido em paralelo.

- **Em memória (.NET):** substituição direta na posição do ring buffer.
- **No ClickHouse:** não existe `UPDATE` barato. A correção é **inserir uma nova versão da linha**; o `ReplacingMergeTree(ingested_at)` mantém a mais recente. A deduplicação é **assíncrona** (ocorre na fusão dos parts), então consultas que exigem garantia usam `FINAL` ou `argMax()`.

> Atenção: `PRIMARY KEY` no ClickHouse é índice esparso, **não** restrição de unicidade — inserir o mesmo bloco duas vezes cria duas linhas. Ver [04 §1.2](./04-persistencia-banco-de-dados.md).

---

## Arquitetura de dados

### RN-09 — Fonte única de verdade

Todo cálculo financeiro ocorre no **backend**, na camada de *Service* (ver o mapeamento MVC em [09 §2](./09-arquitetura-e-stack.md)). O frontend não replica fórmula alguma — recebe valores prontos e apenas formata. Isso garante que backend e painel nunca divirjam, e que o controller permaneça sem lógica de negócio.

### RN-10 — Janela de histórico em memória

Manter os últimos **300 blocos** (≈1 hora de rede). Dados mais antigos saem da memória e permanecem apenas no banco.

**As três janelas do sistema**, todas contidas no buffer de 300 blocos e todas configuráveis:

| Constante | Valor | Usada em | Responde a |
|---|---|---|---|
| `N_fee` | **20** blocos (≈4 min) | RN-02, RF-03 | percentis de *priority fee* |
| `N_cong` | **100** blocos (≈20 min) | RN-04, RF-12 | média móvel de congestionamento |
| `N_buffer` | **300** blocos (≈1 h) | RN-10, RF-14 | gráfico do painel |

> Antes desta versão, cinco requisitos (RF-03, RF-12, RF-14, RN-02, RN-04) diziam apenas *"os últimos N blocos"* sem que N fosse definido em lugar algum. Os três valores acima são a definição única.

### RN-13 — Cliente novo recebe estado imediato

Ao conectar no SSE, o cliente recebe o último snapshot conhecido **antes** de esperar o próximo bloco. Sem isso, o painel fica em branco por até 12 segundos a cada carregamento.

### RN-14 — Banco não serve tempo real

`/current` e a janela quente são servidos **da memória**. O banco atende exclusivamente consultas históricas.

### RN-15 — Retenção

| Dado | Tabela | Retenção |
|---|---|---|
| Blocos brutos | `eth_blocks` | **30 dias**, por `TTL` declarativo |
| Estimativas de custo | `fee_estimates` | 30 dias |
| Amostras de mempool | `mempool_samples` | 7 dias |
| Cotação ETH/USD | `eth_usd_prices` | 90 dias |
| Saúde da ingestão | `ingestion_health` | 14 dias |
| Agregados horários e diários | `eth_fees_rollup` (coluna `granularity`) | indefinidamente |
| Custo diário por operação | `fee_estimates_1d` | indefinidamente |

*Os nomes acima são os do schema real, em [`infra/clickhouse/initdb/`](../../infra/clickhouse/initdb/). Versões anteriores desta regra citavam `blocks`, `fee_stats_hourly` e `fee_stats_daily`, que nunca existiram com esses nomes.*

**Por que 30 dias e não 7:** o D-02 exige percentil sobre janela de 30 dias, e o TAP não impõe limite de armazenamento. A 216 mil linhas/mês, 30 dias de dado bruto é irrelevante em disco. A versão anterior desta regra dizia 7 dias enquanto o DDL usava 90 e a consulta do D-02 usava 30 — três valores para a mesma decisão. **Valor único: 30 dias.**

O parceiro respondeu *"time frame indeterminado, tão aí pra ajudar a gente a definir"* (18/08) — logo, esta é definição nossa e revisável.

---

## Escopo

### RN-12 — Somente leitura

O sistema **nunca** assina, envia ou simula o envio de transações reais. Nenhuma operação consome gas na Mainnet. Restrição contratual do TAP, não escolha técnica.
