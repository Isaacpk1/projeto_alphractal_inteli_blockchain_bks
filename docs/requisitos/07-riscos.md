[← Índice](./README.md)

# 07 — Riscos Técnicos

| # | Risco | Prob. | Impacto | Mitigação |
|---|---|---|---|---|
| R-01 | **Limite do plano RPC estourar** durante testes ou na demo — o MVP consome ~73% da cota gratuita mensal rodando 24/7 | **Alta** | Alto | Uma única chave e um único processo ingerindo (a cota é por conta, não por app); não deixar backend de dev ligado 24/7; evitar `pendingTransactions`; monitorar consumo semanalmente. Orçamento detalhado em [08](./08-orcamento-rpc.md) |
| R-02 | **Latência do provedor** mascarar o "tempo real" | Média | Alto | Medir e **exibir** a defasagem no painel (D-07); trocar de provedor se o p95 passar de 2 s (RNF-01) |
| R-03 | ~~**Erro de precisão numérica** com valores em wei~~ — **muito reduzido** pela troca para .NET | ~~Alta~~ Baixa | Alto | `System.Numerics.BigInteger` é nativo e a Nethereum opera com ele de ponta a ponta. Resta garantir que o React só receba valores já formatados (RN-09) e não faça aritmética de wei |
| R-13 | **Curva da Nethereum**: o ecossistema Web3 documenta quase tudo em `viem`/`ethers`; achar exemplo de subscription em .NET é mais lento | Alta | Alto | Fazer uma *spike* de conexão WebSocket + `newHeads` em .NET **já na semana 1**, antes do protótipo visual. Se não funcionar em 1 dia, escalar com o parceiro (dúvida nº 23) |
| R-14 | **`TOO_MANY_PARTS` no ClickHouse** por inserção linha a linha | Alta se ignorado | Muito alto | Inserção sempre em lote via spool ([04 §1.1](./04-persistencia-banco-de-dados.md)). Falha tipicamente após horas de execução — ou seja, na véspera da demo |
| R-15 | **Duplicatas silenciosas no ClickHouse** — `PRIMARY KEY` não é restrição de unicidade | Média | Médio | `ReplacingMergeTree(ingested_at)`; consultas críticas com `FINAL`/`argMax()`; teste explícito reprocessando o mesmo arquivo de spool |
| R-16 | **Quatro runtimes** (TS, C#, Python, SQL) em ~2 semanas efetivas de código | Alta | Muito alto | Linha de corte definida ([09 §4](./09-arquitetura-e-stack.md)): o painel ao vivo precisa ser demonstrável **sem** Python e **sem** ClickHouse |
| R-17 | **Materialized view sem backfill** — a MV só enxerga linhas inseridas após sua criação | Média | Médio | Criar as MVs **antes** da primeira carga; se já houver dados, `INSERT ... SELECT` manual |
| R-04 | **Mempool subestimada em complexidade** consumir a semana 3 | Alta | Médio | Manter RF-07 como *Could*; decidir na dúvida nº 3 do kick-off, não durante o desenvolvimento |
| R-05 | **Integração visual sem acesso ao design** da Alphractal | Alta | Médio | Destravar na dúvida nº 11 já no kick-off; se não houver design system, propor um e validar por e-mail na semana 1 |
| R-06 | **Chave de API vazar em repositório público** | Média | Muito alto | Chave só no backend via env (RNF-09), `.env.example`, `.gitignore` revisado, varredura de segredos no pré-commit. Histórico do Git é irreversível |
| R-07 | **Resposta do parceiro em 48 h** travar decisão no meio da sprint | ~~Alta~~ Média | Médio | Levar as **29 dúvidas** ao kick-off; para cada pendência, definir um *default* documentado e seguir, ajustando depois. **Probabilidade reduzida:** a conversa de 18/08 respondeu 7 dúvidas fora das reuniões previstas — o canal assíncrono responde melhor que os 48 h do TAP ([doc 10 §5](./10-registro-respostas-parceiro.md)) |
| R-08 | **Rede calma no Demo Day** — sem volatilidade para demonstrar | Média | Médio | Modo replay (D-09) com um período de pico gravado durante o desenvolvimento |
| R-09 | **Escrita no banco bloquear o stream** sob carga | Baixa | Alto | O .NET **nunca escreve no banco**: escreve no spool NDJSON, fora do caminho crítico (RNF-25). Quem insere é o ETL Python, em outro processo. O stream não tem como ser bloqueado por I/O de banco |
| R-10 | **Reorg corromper o histórico** com blocos duplicados | Baixa | Médio | `ReplacingMergeTree(ingested_at)` — a versão mais recente do bloco vence (RN-08, RN-16). ⚠️ **Não usar `UPSERT` nem contar com `PRIMARY KEY` como restrição de unicidade**: no ClickHouse ela é índice esparso e não impede duplicata ([04 §1.2](./04-persistencia-banco-de-dados.md)). Teste com bloco repetido |
| R-18 | **Expectativa de frequência desalinhada** — o parceiro respondeu *"nível diário"* sobre atualização, enquanto o TAP e o RNF-01 exigem tempo real (< 2 s) | Média | **Muito alto** | Interpretação adotada: diário = granularidade de *análise*; tempo real = painel. Documentada em [10 §3.1](./10-registro-respostas-parceiro.md) e implementada como rollup diário ([04 §3.3](./04-persistencia-banco-de-dados.md)). **Primeira pergunta da pauta de 14/09** (dúvida 27). Se a leitura estiver errada, o projeto inteiro muda de forma |
| R-19 | **"4 métricas misteriosas"** — se forem métricas proprietárias da Alphractal ainda não reveladas, o painel será construído sem elas | Média | Alto | Perguntar antes do kick-off (dúvida 26), não em 14/09. Manter RF-22 a RF-27 com layout de cards flexível, para acomodar métricas extras sem redesenhar |
| R-20 | **Escrever direto na instância de produção** da Alphractal, violando restrição do TAP | Baixa | Alto | ClickHouse local via Docker durante todo o projeto ([09 §7](./09-arquitetura-e-stack.md)); troca de instância é decisão do parceiro, pós-entrega |
| R-11 | **Escopo inflar** com itens do backlog de diferenciais | Alta | Alto | Nenhum item **D** iniciado antes de todos os **[M]** fechados (ver [05](./05-backlog-diferenciais.md)) |
| R-12 | **Prazo de 4 semanas** com 2 semanas efetivas de código (semanas 2 e 3) | Alta | Alto | Congelar escopo no fim da semana 1; tratar semana 4 como estabilização e ensaio da demo, não como desenvolvimento |

---

## Riscos que valem atenção especial

**R-16 (quatro runtimes)** passou a ser o principal risco do projeto. Não é a dificuldade de nenhuma tecnologia isolada — é que cada fronteira entre elas (.NET→spool, spool→Python, Python→ClickHouse, .NET→ClickHouse, .NET→React) é um lugar onde se perde um dia. São cinco fronteiras em duas semanas de código. A linha de corte de [09 §4](./09-arquitetura-e-stack.md) existe exatamente para isso.

**R-14 e R-13 são os dois que mordem tarde.** Ambos funcionam perfeitamente nos primeiros testes e falham depois: a Nethereum quando você precisa de algo além do exemplo do README, e o ClickHouse depois de algumas horas acumulando parts. Por isso as duas mitigações são *antecipar a descoberta* — spike de Nethereum na semana 1, e deixar o ingestor rodando ininterrupto por 24 h ainda na semana 3.

**R-03 (precisão numérica)** deixou de ser o risco mais insidioso: era um problema do `number` de 64 bits do JavaScript, e o .NET tem `BigInteger` nativo. A troca de stack resolveu de graça um risco que exigiria disciplina constante em Node.

**R-18 é o risco novo mais grave**, e é de comunicação, não de tecnologia. Não custa nada mitigar — uma frase no kick-off — e custa o projeto inteiro se for ignorado. Riscos de alinhamento de expectativa não aparecem em teste nenhum: aparecem na apresentação final, quando o parceiro vê algo diferente do que imaginou ter pedido.

**R-11 (inflação de escopo)** segue provável. O backlog de diferenciais existe para dar lugar formal às boas ideias sem que invadam a sprint.

**R-12 (prazo)** merece honestidade: a semana 1 é pesquisa e protótipo, a semana 4 é demo. Restam duas semanas de código real — agora distribuídas por quatro tecnologias.
