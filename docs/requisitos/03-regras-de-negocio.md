[← Índice](./README.md)

# 03 — Regras de Negócio (RN)

> Todas as regras abaixo vivem na **camada de serviço do backend** (RN-09). O frontend apenas formata e exibe.

---

## Cálculo de custo

### RN-01 — Custo da transação (EIP-1559)

```
custo_wei = (baseFeePerGas + priorityFee) × gasLimit
```

O `baseFee` é definido pelo protocolo e **queimado**; a *priority fee* (gorjeta ao validador) é a única variável que o usuário controla e é o que define a velocidade de inclusão.

### RN-02 — Faixas de velocidade

Derivadas dos percentis de *priority fee* retornados por `eth_feeHistory` sobre os últimos N blocos:

| Faixa | Percentil | Expectativa |
|---|---|---|
| Lento | p25 | inclusão em vários blocos |
| Padrão | p50 | próximos blocos |
| Rápido | p90 | próximo bloco |

*(percentis e janela N a validar com o parceiro — dúvida nº 8)*

### RN-03 — Conversão para USD

```
custo_usd = custo_eth × preço_ETH_USD
```

A cotação é atualizada no máximo a cada **60 s**. Se estiver defasada há mais de **5 min**, o valor em USD deve ser exibido como desatualizado.

### RN-06 — Unidades e precisão

- Exibição: gwei com 2 casas · USD com 2 casas · ETH com até 6 casas.
- **Toda aritmética interna usa `bigint` em wei.** Nunca `float`/`number` — a precisão de ponto flutuante corrompe valores em wei e propaga erro para o custo em USD. A conversão para decimal acontece somente na formatação final.

### RN-11 — Gas limits de referência

Constantes de configuração, documentadas e ajustáveis sem alterar código de negócio:

| Tipo de transação | Gas limit de referência |
|---|---|
| Transferência de ETH | 21.000 |
| Transferência ERC-20 | ~65.000 |
| Aprovação (`approve`) | ~46.000 |
| Swap em DEX | ~150.000 |
| Mint de NFT | ~85.000 |

*(valores a validar com o parceiro — dúvida nº 7)*

---

## Estado da rede

### RN-04 — Índice de congestionamento

Compara a `baseFee` atual com a média móvel dos últimos N blocos:

| Faixa | Relação com a média | Rótulo |
|---|---|---|
| < 70% | bem abaixo | Baixo |
| 70–130% | dentro do normal | Normal |
| 130–200% | acima | Alto |
| > 200% | muito acima | Extremo |

**Procedência:** o termo *"saúde da rede"* vem literalmente do TAP — *"uma métrica de 'saúde' atual da rede"* (seção 2, Problema). O **método de cálculo e as faixas acima não vêm do TAP**: são proposta nossa, preenchendo uma lacuna. Daí a marcação *a validar* (dúvida nº 6).

**Limitação conhecida desta regra.** Por comparar com uma média móvel curta, ela mede **variação**, não **nível**. Num período sustentado de taxas altas, a média móvel acompanha a subida e o indicador volta a marcar "Normal" — mesmo com o gas historicamente caro. É um ponto cego real, e o diferencial **D-02** (percentil histórico) existe para cobri-lo. Os dois são **complementares, não substitutos**: esta regra responde *"está subindo agora?"*; o percentil responde *"está caro em termos históricos?"*.

### RN-05 — Projeção da base fee do próximo bloco

Regra determinística do protocolo, não previsão estatística:

- `gasUsed > gasLimit / 2` → base fee sobe, no máximo **+12,5%**
- `gasUsed < gasLimit / 2` → base fee cai, no máximo **−12,5%**
- `gasUsed = gasLimit / 2` → base fee permanece

### RN-07 — Dado obsoleto (*stale*)

Se não chegar bloco novo em mais de **60 s** (≈5 blocos), o painel deve sair do estado "Ao vivo" e sinalizar dados desatualizados. Exibir número velho como se fosse atual é pior do que exibir erro — é justamente o problema que o projeto veio resolver.

### RN-08 / RN-16 — Reorganização de cadeia (*reorg*)

Se chegar um bloco com número **menor ou igual** ao último processado, o registro existente é **sobrescrito**, nunca duplicado nem mantido em paralelo. O histórico é corrigido tanto em memória quanto no banco.

---

## Arquitetura de dados

### RN-09 — Fonte única de verdade

Todo cálculo financeiro ocorre no **backend**. O frontend não replica fórmula alguma — recebe valores prontos e apenas formata. Isso garante que backend e painel nunca divirjam.

### RN-10 — Janela de histórico em memória

Manter os últimos **~300 blocos** (≈1 hora de rede). Dados mais antigos saem da memória e permanecem apenas no banco.

### RN-13 — Cliente novo recebe estado imediato

Ao conectar no SSE, o cliente recebe o último snapshot conhecido **antes** de esperar o próximo bloco. Sem isso, o painel fica em branco por até 12 segundos a cada carregamento.

### RN-14 — Banco não serve tempo real

`/current` e a janela quente são servidos **da memória**. O banco atende exclusivamente consultas históricas.

### RN-15 — Retenção

Dados bloco a bloco por **7 dias** (configurável); agregados horários mantidos indefinidamente.

---

## Escopo

### RN-12 — Somente leitura

O sistema **nunca** assina, envia ou simula o envio de transações reais. Nenhuma operação consome gas na Mainnet. Restrição contratual do TAP, não escolha técnica.
