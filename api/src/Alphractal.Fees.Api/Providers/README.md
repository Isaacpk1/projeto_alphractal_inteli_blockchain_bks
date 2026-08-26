# Providers — o mundo lá fora

Adaptadores para serviços externos. Trocáveis sem tocar em `Services/`.

- **Nethereum** — cliente WebSocket da Alchemy, usado pelo `BackgroundServices/`.
- **Cotação ETH/USD** — fonte de preço para converter custo em dólar.

**Regras**

- Reconexão com backoff e log é responsabilidade daqui, não do serviço que consome.
- Cada provider tem interface própria: o `Services/` depende da interface, nunca
  da biblioteca. É o que permite testar sem rede.
- Chave de API vem de configuração/`.env`. Nunca hard-coded, nunca em `appsettings.json`.
