# transform/ — linha do spool → linha do ClickHouse

Recebe o registro NDJSON que a API .NET escreveu e devolve a tupla pronta para
`writer.py`. Sem I/O aqui: função pura, entra dict, sai linha.

**As regras que não podem ser quebradas**

1. **Unidade é wei**, inteiro, em toda coluna de taxa. Nunca gwei, nunca float.
   Python tem inteiro de precisão arbitrária — use e não converta para `float`.
2. **Timestamp é UTC.** `block_timestamp` vem da rede, não de `datetime.now()`.
3. **O formato é o de `contract.py`**, que espelha `infra/clickhouse/initdb/002_tables.sql`.
   Coluna, tipo e ordem. Divergiu, o painel mostra número errado sem erro nenhum.
