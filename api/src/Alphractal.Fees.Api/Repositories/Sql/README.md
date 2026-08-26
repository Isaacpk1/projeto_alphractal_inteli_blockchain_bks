# Repositories/Sql — as queries, isoladas do C#

Um arquivo `.sql` por consulta, carregado como recurso embutido.

**Regras**

- Parâmetro é do lado do servidor: `{nome:Tipo}` na sintaxe do ClickHouse. É isso
  que protege contra injeção — nunca concatene string.
- Só views `v_*`. Se você precisou de `FINAL` ou de `quantilesMerge(...)` aqui,
  a lógica está no lugar errado: ela pertence ao `004_views.sql` em `infra/`.
- Métrica nova = view nova em `infra/`, não SQL novo aqui.
