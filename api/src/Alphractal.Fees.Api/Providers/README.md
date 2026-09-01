# Providers — o mundo lá fora

Adaptadores para serviços externos. Trocáveis sem tocar em `Services/`.

- **Nethereum** — WebSocket `newHeads` e HTTP para `eth_feeHistory`, contagem e
  `eth_getBlockReceipts`. O total pago no bloco é a soma exata de
  `gasUsed × effectiveGasPrice` dos recibos.
- **Cotação ETH/USD** — fonte de preço para converter custo em dólar.

**Regras**

- Reconexão com backoff e log é responsabilidade daqui, não do serviço que consome.
- Cada provider tem interface própria: o `Services/` depende da interface, nunca
  da biblioteca. É o que permite testar sem rede.
- Chave de API vem de configuração/`.env`. Nunca hard-coded, nunca em `appsettings.json`.
- Falha de recibos degrada o histórico (`total_fee_wei = 0`) sem interromper o
  stream ao vivo; a causa fica registrada sem imprimir a URL que contém a chave.
