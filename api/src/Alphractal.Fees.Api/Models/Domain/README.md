# Models/Domain — o "M" do MVC

Entidades do domínio: bloco, janela quente, amostra de mempool, estimativa de taxa.

**Regras**

- Valores em wei são `System.Numerics.BigInteger`. Nunca `double`, nunca `long` — é
  o que elimina o risco R-03 (perda de precisão).
- Timestamp é `DateTimeOffset` em UTC, vindo da rede, não do relógio da máquina.
- Não sabe que a web existe: nada de atributos de serialização aqui.
- Muda quando **a rede ou o banco** muda. Se está mudando porque o front pediu
  outro formato, o lugar é `Models/Responses/`.
