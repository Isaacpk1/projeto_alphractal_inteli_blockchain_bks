# ADR-004 — O total de taxas vem do recibo, não da mediana da gorjeta

- **Status:** **aceita**
- **Data:** 01/09/2026
- **Contexto:** [ADR-003](003-metricas-historicas.md) · [09 §3](../requisitos/09-arquitetura-e-stack.md)

---

## Contexto

A aba de métricas históricas exibia "Total Fees" reconstruindo o número a partir
do que o rollup guardava:

```
total_eth = burned_eth × (base_fee_avg + priority_fee_p50) / base_fee_avg
```

Para 27/08/2026 isso deu **87,70 ETH**. A plataforma de referência mostrava
**181,45 ETH** para o mesmo dia — nosso número estava 51,7% abaixo.

A diferença não é de preço nem de fuso: os dados do dia eram 41,28 ETH queimados,
base fee média de 0,1917 gwei e `priority_fee_p50` de 0,2155 gwei, e a conta
acima reproduz 87,70 exatamente. O erro está na premissa.

`priority_fee_p50` é a **mediana** da gorjeta, e a distribuição de gorjetas tem
cauda pesada: contratos, MEV e liquidações pagam muito acima da mediana **e**
consomem muito gas. Tratar a mediana como média subestima a parcela de gorjeta —
neste caso, pela metade. Não existe fator de correção estável: a razão entre
média e mediana muda com o regime da rede, e é maior justamente quando a base
fee está baixa e a gorjeta domina o custo.

O alvo foi confirmado contra fonte independente antes da mudança. A métrica
`FeeTotNtv` da Coin Metrics — soma de `gasUsed × effectiveGasPrice` de todas as
transações — dá **181,446106 ETH** para 27/08/2026, e a série da semana inteira
coincide com a da plataforma de referência, inclusive o valor de 30/08
(127,06 ETH). Ou seja: o número publicado é o total efetivamente pago, e é
reprodutível.

## Decisão

**Persistir `total_fee_wei` por bloco, somando os recibos, e parar de estimar.**

```
total_fee_wei = Σ (receipt.gasUsed × receipt.effectiveGasPrice)
```

- `eth_blocks.total_fee_wei` e `eth_fees_rollup.total_fee_wei` (`UInt128`, como
  `burned_wei` — o produto excede 2⁶⁴).
- As views `v_eth_fees_1h`, `v_eth_fees_1d` e `v_latest_block` expõem
  `total_fee_eth`.
- Coleta por `eth_getBlockReceipts`: **uma chamada por bloco** na ingestão ao
  vivo, e lote próprio no backfill (`--recibos-por-lote`, padrão 8).
- O front usa `total_fee_eth` quando ele é maior que zero.

## Consequências

**O `0` significa "não coletado", não "não houve taxa".** Os blocos ingeridos
antes desta mudança ficam zerados, e um bucket com blocos e taxa zero não existe
na prática. Por isso o front mantém a estimativa antiga como *fallback* — para
não exibir zero — e o backfill do período visível é o que a elimina. O contrato
do ETL aceita a coluna ausente pelo mesmo motivo: um arquivo de spool escrito
pela versão anterior carrega centenas de blocos e seria rejeitado inteiro por um
campo novo.

**A resposta de recibos é cara.** Cada recibo traz todos os logs da transação, e
um bloco cheio passa de 1 MB — ordens de grandeza acima do cabeçalho. Daí o lote
separado no backfill: pedir os recibos dos mesmos 100 blocos do lote de
cabeçalhos traria dezenas de MB numa única resposta. Um dia inteiro são ~7.200
blocos; refazer 30 dias custa tempo e cota de RPC, e é uma decisão consciente,
não um efeito colateral. `--recibos-por-lote 0` desliga a coleta para quem
quiser só a série de base fee.

**Uma chamada RPC a menos por bloco na ingestão ao vivo.** Os recibos já trazem
uma entrada por transação, então `tx_count` sai da mesma resposta e
`eth_getBlockTransactionCountByNumber` só é chamado quando os recibos falham.

**Falha de recibo degrada, não interrompe.** O bloco vai para o spool com
`total_fee_wei = 0` e o painel ao vivo — que não usa este número — segue
intacto. O caminho quente continua sendo o requisito de 2 s.
