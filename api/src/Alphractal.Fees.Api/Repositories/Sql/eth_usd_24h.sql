-- Cotacao atual e de 24 h atras, para a variacao percentual.
SELECT preco_atual, observado_em, preco_24h, observado_em_24h, amostras_24h
FROM v_eth_usd_24h
