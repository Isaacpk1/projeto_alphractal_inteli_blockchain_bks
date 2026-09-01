# backfill/ — carga histórica e reconciliação

Popula 30 dias de blocos para habilitar os diferenciais D-02 (percentis) e D-04
(heatmap), e detecta lacunas comparando o que está no ClickHouse com o RPC.

**Regras**

- **RPC por HTTP, nunca WebSocket.** A assinatura ao vivo de `newHeads` é da API
  .NET. Abrir uma segunda aqui dobra o consumo do orçamento de RPC
  ([08](../../../../docs/requisitos/08-orcamento-rpc.md)).
- Roda sob demanda pelo comando `alphractal-etl backfill`. É script, não serviço.
- Inserção em lote, sempre. Um `INSERT` por bloco gera uma *part* por `INSERT` e o
  servidor trava com `TOO_MANY_PARTS` em poucas horas.
- Gera NDJSON em `spool/ready/` e reutiliza exatamente o mesmo contrato e writer
  da ingestão normal.
- `eth_feeHistory` é ancorado no bloco final do lote. O valor adicional de
  `baseFeePerGas` é a taxa do próximo bloco e não deve ser projetado novamente.
- `eth_getBlockReceipts` usa lote próprio (`--recibos-por-lote`, padrão 8), pois
  os logs tornam a resposta muito maior que o cabeçalho. A soma de
  `gasUsed × effectiveGasPrice` preenche `total_fee_wei` sem estimar gorjetas.
