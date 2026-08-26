# tests/ — validação sem banco

`test_contract.py` valida uma linha contra `contract.py` **sem precisar de
ClickHouse no ar**. É o teste que pega divergência de schema antes do runtime.

```bash
cd etl
source .venv/bin/activate
pytest
```

**O que precisa de teste, em ordem de risco**

1. **Unidade e tipo de cada coluna** contra `002_tables.sql`. É o erro mais silencioso.
2. **Timestamp em UTC**, incluindo bloco na virada do dia.
3. **Parsing do NDJSON do spool** — arquivo truncado, linha vazia, campo faltando.
4. **Idempotência:** reprocessar o mesmo arquivo de `ready/` não pode duplicar dado.
