[← Índice](./README.md)

# 08 — Orçamento de Consumo RPC (Alchemy)

> Fonte: documentação oficial da Alchemy, consultada em **18/08/2026**. Preços e limites mudam — reconferir antes do kick-off.
> Os cálculos abaixo são **estimativas de ordem de grandeza**, não medições. Validar com o painel de uso real na semana 2.

---

## 1. Como a Alchemy cobra

A Alchemy cobra em **Compute Units (CU)**, com dois modelos distintos:

| Tipo de chamada | Modelo de cobrança |
|---|---|
| Métodos JSON-RPC comuns | **Custo fixo por método** |
| Subscriptions (WebSocket) e Webhooks | **Por banda: 0,04 CU por byte entregue** |

Custos por método relevantes ao projeto:

| Método | CU por chamada |
|---|---|
| `eth_subscribe` | 10 (na abertura) |
| `eth_feeHistory` | 10 |
| `eth_maxPriorityFeePerGas` | 10 |
| `eth_getBlockByNumber` | 20 |
| `eth_gasPrice` | 20 |

### Limites do plano gratuito

| Limite | Valor |
|---|---|
| CU por mês | **30.000.000** (por conta, não por app) |
| Throughput | **500 CUPS** (CU por segundo, por aplicação) |
| Aplicações | 5 |

**WebSockets estão disponíveis em todos os planos**, inclusive no gratuito. O que é gated para planos pagos são as **Debug API e Trace API**.

---

## 2. Correção importante

> ❌ **Afirmação anterior (incorreta):** *"assinar `pendingTransactions` costuma ser bloqueado em planos básicos"*.
>
> ✅ **Correto:** a subscription **não é bloqueada por plano** — ela é cobrada **por byte entregue**, e o volume da mempool do Ethereum torna o custo proibitivo no plano gratuito. O efeito prático é o mesmo (inviável no free tier), mas a causa é econômica e de throughput, não uma trava comercial.

Essa distinção importa na conversa com o parceiro: **se a Alphractal tiver um plano pago, `pendingTransactions` volta a ser viável** — a pergunta muda de "é possível?" para "quanto vocês aceitam gastar?".

---

## 3. Estimativa de consumo por funcionalidade

Premissas: 1 bloco a cada ~12 s (7.200 blocos/dia); Ethereum processando ~15 transações/s.

| Funcionalidade | Fluxo | CU/dia (est.) | CU/mês (est.) | % do free tier |
|---|---|---|---|---|
| `newHeads` (RF-01) | 1 evento/12 s, header ~1,8 KB (o `logsBloom` sozinho ocupa ~1 KB) | ~518 k | **~15,5 M** | ~52% |
| `eth_feeHistory` (RF-03) | 1 chamada por bloco × 10 CU | ~72 k | **~2,2 M** | ~7% |
| `eth_getBlockByNumber` com transações (D-06) | 1 chamada por bloco × 20 CU | ~144 k | **~4,3 M** | ~14% |
| **Subtotal do MVP + D-06** | | ~734 k | **~22 M** | **~73%** |
| `newPendingTransactions` (só hashes) | ~15/s × ~80 B | ~4,1 M | ~124 M | **413%** |
| `alchemy_pendingTransactions` (objetos completos) | ~15/s × ~600 B | ~31 M | ~930 M | **3.100%** |

### Leitura dos números

- **O MVP cabe no plano gratuito, mas com pouca folga:** ~73% da cota mensal com uma única instância rodando 24/7.
- **Mempool só com hashes** queima a cota mensal inteira em **~7 dias**.
- **Mempool com objetos completos** queima a cota mensal em **menos de 24 horas** — e a ~360 CU/s fica perigosamente perto do teto de 500 CUPS, ou seja, seria estrangulada por rate limit antes mesmo de esgotar a cota.

---

## 4. Consequências para o projeto

| Decisão | Justificativa |
|---|---|
| **RF-07 (mempool) permanece [C]** | Inviável no free tier. Só reconsiderar se o parceiro fornecer chave de plano pago (dúvida nº 1). |
| **Uma única chave, um único processo ingerindo** | A cota é **por conta**, não por app. Dois desenvolvedores rodando o backend em paralelo dobram o consumo e estouram o mês. |
| **Não deixar o backend rodando 24/7 em máquina de dev** | ~52% da cota vai só em `newHeads`. Ligar sob demanda durante o desenvolvimento. |
| **D-06 é barato, mas não gratuito** | `eth_getBlockByNumber` custa 20 CU fixos por chamada, independentemente do tamanho da resposta — bem mais barato do que parecia. Ainda assim, seus ~14% empurram o total para 73% e derrubam a margem do RNF-05 abaixo dos 30% exigidos. Ver §5. |
| **D-05 (L1 vs L2) multiplica o consumo** | Cada rede adicional é outro `newHeads`. Quatro redes ≈ 62 M CU/mês → **exige plano pago**. |
| **Plano pago é barato se necessário** | PAYG a US$ 0,45/M CU: o MVP inteiro rodando 24/7 sai por **~US$ 10/mês**. Vale mencionar ao parceiro — é um custo desprezível para eles e destrava mempool e multi-rede. |

---

## 5. Requisito derivado

**RNF-05 (revisado):** o consumo de CU deve caber no plano contratado, com margem de segurança de pelo menos 30%. O sistema deve registrar em log o volume estimado consumido por dia, e o consumo real deve ser conferido no painel do provedor **ao fim da semana 2**, antes de qualquer decisão sobre itens do backlog que aumentem o tráfego RPC.

### Onde o próprio requisito aperta ⚠️

| Escopo | CU/mês | Consumo | Margem | RNF-05 |
|---|---|---|---|---|
| MVP obrigatório (`newHeads` + `eth_feeHistory`) | ~17,7 M | 59% | **41%** | ✅ atende |
| MVP + D-06 (`eth_getBlockByNumber`) | ~22,0 M | 73% | **27%** | ❌ viola |

O número de 73% que circula neste documento **já inclui D-06**. O MVP puro cabe com folga; é o diferencial de atribuição de picos que estoura a margem. Consequência prática: **D-06 só é aprovado se houver plano pago, ou se o ingestor não rodar 24/7** durante o desenvolvimento. Decidir ao fim da semana 2, com consumo medido — não estimado.

---

## Fontes

- [Alchemy — Pricing Plans](https://www.alchemy.com/docs/reference/pricing-plans)
- [Alchemy — Compute Unit Costs](https://www.alchemy.com/docs/reference/compute-unit-costs)
- [Alchemy — How Are Websockets Priced?](https://www.alchemy.com/support/how-are-websockets-priced)
- [Alchemy — Free Tier Details](https://www.alchemy.com/support/free-tier-details)
