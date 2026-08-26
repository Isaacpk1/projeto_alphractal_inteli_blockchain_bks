# BackgroundServices — a ingestão contínua

`BlockIngestionService`, registrado como `IHostedService`. Assina `newHeads` via
Nethereum, alimenta a janela quente e escreve o spool.

**Por que não é um controller**

Controller responde a request. Isto aqui não tem request: é um processo que roda do
start ao shutdown da aplicação. Ver [09 §2](../../../../docs/requisitos/09-arquitetura-e-stack.md).

**Regras**

- Esta é a **única** conexão RPC ao vivo do projeto inteiro. O ETL lê o spool, não
  a rede. Uma segunda assinatura de `newHeads` dobra o consumo do orçamento de RPC.
- Nenhum cálculo aqui: recebe o bloco, entrega para `Services/`.
- Queda de conexão não pode derrubar a aplicação — reconecta e registra a lacuna
  em `ingestion_health`.
