# Repositories — ClickHouse (histórico) e spool (saída para o ETL)

Duas responsabilidades, ambas de I/O:

1. **Leitura no ClickHouse** para `/api/history` e agregados do caminho frio.
2. **Escrita do spool NDJSON** que o ETL consome — append-only, fora do caminho
   crítico (RNF-25).

**Regras**

- **O caminho quente não passa por aqui** (RN-14). O SSE é servido pela janela em
  memória de `Services/`. Consulta ao banco tem frescor de ~1 minuto e mataria o RNF-01.
- A API só consulta views `v_*`. Nunca tabela base direto.
- Nenhuma conversão de unidade em C#: a view já devolve gwei, ETH e USD.
- SQL não mora em string dentro do `.cs` — vai em `Sql/`.
- Convenção do spool: escreve em `spool/pending/blocks-YYYYMMDD-HHMM.ndjson`, fecha
  ao virar o minuto e move para `spool/ready/`. Só o ETL toca em `ready/`.
