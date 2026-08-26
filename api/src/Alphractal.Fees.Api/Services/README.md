# Services — onde vive toda a matemática

Regras de negócio RN-01 a RN-05, janela quente de 300 blocos (RN-10) e o
broadcaster que distribui para os assinantes SSE.

**Regras**

- **Toda** a matemática está aqui (RN-09). Controller não calcula, repositório não calcula.
- `BigInteger` para qualquer valor em wei.
- A janela quente é um ring buffer **em memória**. O fan-out para N clientes é
  `System.Threading.Channels.Channel<T>`: um produtor (a ingestão), N consumidores (SSE).
- Não conhece HTTP e não conhece ClickHouse. Recebe e devolve tipos de `Models/`.
