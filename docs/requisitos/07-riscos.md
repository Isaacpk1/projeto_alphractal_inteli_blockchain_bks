[← Índice](./README.md)

# 07 — Riscos Técnicos

| # | Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|---|
| R-01 | **Limite do plano RPC estourar** durante testes ou na demo | Média | Alto | Uma única conexão compartilhada (RF-19), cache agressivo, evitar `pendingTransactions`; monitorar consumo diariamente (RNF-05) |
| R-02 | **Latência do provedor** mascarar o "tempo real" | Média | Alto | Medir e **exibir** a defasagem no painel (D-07); trocar de provedor se o p95 passar de 2 s (RNF-01) |
| R-03 | **Erro de precisão numérica** com valores em wei | Alta | Alto | `bigint` obrigatório em toda aritmética (RN-06); testes unitários com valores extremos (RNF-15) |
| R-04 | **Mempool subestimada em complexidade** consumir a semana 3 | Alta | Médio | Manter RF-07 como *Could*; decidir na dúvida nº 3 do kick-off, não durante o desenvolvimento |
| R-05 | **Integração visual sem acesso ao design** da Alphractal | Alta | Médio | Destravar na dúvida nº 11 já no kick-off; se não houver design system, propor um e validar por e-mail na semana 1 |
| R-06 | **Chave de API vazar em repositório público** | Média | Muito alto | Chave só no backend via env (RNF-09), `.env.example`, `.gitignore` revisado, varredura de segredos no pré-commit. Histórico do Git é irreversível |
| R-07 | **Resposta do parceiro em 48 h** travar decisão no meio da sprint | Alta | Médio | Levar todas as 20 dúvidas ao kick-off; para cada pendência, definir um *default* documentado e seguir, ajustando depois |
| R-08 | **Rede calma no Demo Day** — sem volatilidade para demonstrar | Média | Médio | Modo replay (D-09) com um período de pico gravado durante o desenvolvimento |
| R-09 | **Escrita no banco bloquear o stream** sob carga | Baixa | Alto | Persistência fora do caminho crítico, assíncrona/em lote (RNF-25); SQLite em modo WAL (RNF-29) |
| R-10 | **Reorg corromper o histórico** com blocos duplicados | Baixa | Médio | `block_number` como chave primária + `UPSERT` (RN-08, RN-16); teste com bloco repetido |
| R-11 | **Escopo inflar** com itens do backlog de diferenciais | Alta | Alto | Nenhum item **D** iniciado antes de todos os **[M]** fechados (ver [05](./05-backlog-diferenciais.md)) |
| R-12 | **Prazo de 4 semanas** com 2 semanas efetivas de código (semanas 2 e 3) | Alta | Alto | Congelar escopo no fim da semana 1; tratar semana 4 como estabilização e ensaio da demo, não como desenvolvimento |

---

## Riscos que valem atenção especial

**R-03 (precisão numérica)** é o mais insidioso: um `number` do JavaScript segura com segurança até 2⁵³, e valores em wei passam disso rotineiramente. O erro não estoura — ele silenciosamente devolve um custo em USD errado, que é exatamente o que o produto promete acertar. É o tipo de bug que só aparece na apresentação.

**R-11 (inflação de escopo)** é o mais provável. O backlog de diferenciais existe justamente para dar um lugar formal às boas ideias sem que elas invadam a sprint.

**R-12 (prazo)** merece honestidade: a semana 1 é pesquisa e protótipo, a semana 4 é demo. Restam duas semanas de código real. Todo o dimensionamento de escopo deve partir daí.
