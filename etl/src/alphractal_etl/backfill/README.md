# backfill/ — carga histórica e reconciliação

Popula 30 dias de blocos para habilitar os diferenciais D-02 (percentis) e D-04
(heatmap), e detecta lacunas comparando o que está no ClickHouse com o RPC.

**Regras**

- **RPC por HTTP, nunca WebSocket.** A assinatura ao vivo de `newHeads` é da API
  .NET. Abrir uma segunda aqui dobra o consumo do orçamento de RPC
  ([08](../../../../docs/requisitos/08-orcamento-rpc.md)).
- Roda sob demanda, não em loop. É script, não serviço.
- Inserção em lote, sempre. Um `INSERT` por bloco gera uma *part* por `INSERT` e o
  servidor trava com `TOO_MANY_PARTS` em poucas horas.
- Reinserir bloco já existente é seguro: `eth_blocks` é `ReplacingMergeTree` com
  chave `block_number`. Idempotência é do schema, não sua.
