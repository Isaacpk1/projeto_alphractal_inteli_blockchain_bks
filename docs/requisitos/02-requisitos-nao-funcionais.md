[← Índice](./README.md)

# 02 — Requisitos Não Funcionais (RNF)

RNF-01 a RNF-31. IDs cancelados são mantidos riscados e nunca reaproveitados.

---

## 1. Desempenho e tempo real

| ID | Requisito | Como verificar |
|---|---|---|
| RNF-01 | O dado deve aparecer no painel em até **2 segundos** após a emissão do bloco pelo nó RPC. | Diferença entre `ingested_at` e a renderização, exibida no próprio painel (ver D-07) |
| RNF-02 | O endpoint de snapshot deve responder em **< 300 ms (p95)**, servido do estado em memória, sem chamada síncrona ao RPC. | Teste de carga simples (`autocannon`/`k6`) |
| RNF-03 | O frontend não deve re-renderizar a árvore inteira a cada atualização; atualizações devem ser isoladas por componente (memoização / estado local). | React DevTools Profiler — nenhum render desnecessário acima do card |
| RNF-04 | O sistema deve suportar pelo menos **100 clientes SSE simultâneos** com uma única conexão RPC. | Script de 100 conexões `EventSource` mantidas por 10 min |
| RNF-05 | O consumo de Compute Units deve caber no plano contratado com **margem de segurança ≥ 30%**. O MVP obrigatório (`newHeads` + `eth_feeHistory`) consome ~17,7 M CU/mês contra 30 M do plano gratuito — **41% de margem, dentro do limite**. Somando D-06, sobe para ~22 M e a margem cai a **27%, abaixo do exigido**: D-06 só entra com plano pago ou com o ingestor não rodando 24/7. Ver [08 — Orçamento RPC](./08-orcamento-rpc.md). | Painel de uso do provedor conferido ao fim da semana 2, antes de aprovar qualquer item do backlog que aumente o tráfego |

## 2. Confiabilidade

| ID | Requisito |
|---|---|
| RNF-06 | Reconexão ao RPC em até **5 s** após queda, com *backoff* limitado e sem vazamento de listeners. |
| RNF-07 | Falha do serviço de cotação USD não pode derrubar o fluxo on-chain — degradação graciosa: exibir apenas gwei e sinalizar cotação indisponível. |
| RNF-08 | Nenhuma exceção não tratada pode encerrar o processo do backend; erros devem ser logados e isolados por camada. |
| RNF-26 | Falha de escrita no banco não pode interromper o stream ao vivo — apenas registrar o erro. |

## 3. Segurança

| ID | Requisito |
|---|---|
| RNF-09 | Chaves de API do provedor RPC devem residir **apenas no backend**, em variáveis de ambiente, nunca no bundle do frontend nem no repositório. |
| RNF-10 | O backend deve aplicar CORS restrito às origens autorizadas e *rate limiting* nos endpoints públicos. |
| RNF-11 | O sistema é estritamente *read-only*: não deve conter chave privada, carteira, assinatura ou envio de transação. |
| RNF-12 | O repositório deve conter `.env.example` e `.gitignore` adequados; segredos jamais versionados. |

> Como o repositório é **público sob MIT**, um vazamento de chave é irreversível — o histórico do Git preserva o segredo mesmo após remoção. Recomenda-se *hook* de pré-commit com varredura de segredos.

## 4. Manutenibilidade e qualidade

| ID | Requisito |
|---|---|
| RNF-13 | Tipagem estrita em todas as camadas: **TypeScript `strict`** no React; **`<Nullable>enable</Nullable>` + `TreatWarningsAsErrors`** no .NET; **`mypy`** no ETL Python. O contrato de dados do SSE deve ter uma definição única e versionada, gerada ou espelhada entre C# e TypeScript. |
| RNF-14 | Arquitetura em camadas desacopladas — *provider* (RPC) → *service* (regras) → *repository* (dados) → *transport* (SSE/HTTP) — permitindo trocar provedor, banco ou transporte sem reescrever a lógica de negócio. **No .NET essas camadas são realizadas sobre a estrutura MVC pedida pelo parceiro**; o mapeamento está em [09 §2](./09-arquitetura-e-stack.md). |
| RNF-31 | O backend deve seguir **estrutura MVC** (ASP.NET Core), com controllers isentos de lógica de negócio: toda regra vive na camada de *service* (RN-09). Definido pelo parceiro em 18/08 ([doc 10](./10-registro-respostas-parceiro.md)). |
| RNF-30 | O caminho quente (.NET → SSE → React) não deve depender de ClickHouse nem do ETL Python: com o banco fora do ar, o painel ao vivo continua funcionando, perdendo apenas as visões históricas. |
| RNF-15 | Testes unitários automatizados para as regras de cálculo (RN-01 a RN-06), com blocos mockados, incluindo casos de borda (base fee no mínimo, bloco cheio, bloco vazio). |
| RNF-16 | Lint e formatação padronizados (ESLint + Prettier) e mensagens de commit convencionais. |
| RNF-17 | README com instruções de instalação, execução e descrição da arquitetura; endpoints documentados. |
| RNF-18 | Logs estruturados (nível, timestamp, contexto) para conexão RPC, blocos processados e erros. |
| RNF-25 | A escrita no banco deve ocorrer **fora do caminho crítico** do SSE: o evento vai aos clientes primeiro; a persistência acontece de forma assíncrona/em lote. |
| RNF-27 | O acesso a dados deve ficar atrás de uma interface de *repository*, permitindo trocar a instância de ClickHouse (Docker local ↔ instância da Alphractal) por configuração, sem alterar a camada de serviço. |
| RNF-28 | Migrações de schema devem ser versionadas em arquivos no repositório, nunca aplicadas manualmente no banco. As *materialized views* precisam ser criadas **antes** da primeira carga (ver R-17). |
| ~~RNF-29~~ | ~~Com SQLite, habilitar modo **WAL**~~ — **cancelado**. Era específico do SQLite, revogado pela adoção do ClickHouse. O problema equivalente aqui é a inserção em lote (R-14, [04 §1.1](./04-persistencia-banco-de-dados.md)). |

## 5. Usabilidade e acessibilidade

| ID | Requisito |
|---|---|
| RNF-19 | Estados críticos (congestionamento, conexão) não podem ser comunicados **apenas por cor** — usar também texto e/ou ícone. |
| RNF-20 | Contraste mínimo **WCAG AA** sobre o tema escuro da plataforma. |
| RNF-21 | Compatível com navegadores modernos (Chrome, Edge, Firefox, Safari) e utilizável em telas a partir de 360 px. |

## 6. Portabilidade e legais

| ID | Requisito |
|---|---|
| RNF-22 | Execução local via **`docker-compose up`** — obrigatório, não opcional: ClickHouse exige container, e são quatro componentes (React, .NET, Python, ClickHouse). Um `README` deve permitir a um avaliador subir tudo sem conhecimento prévio da stack. |
| RNF-23 | Código publicado em repositório público sob licença **MIT**. |
| RNF-24 | O sistema não coleta nem armazena dados pessoais de usuários — fora do escopo de LGPD. |
