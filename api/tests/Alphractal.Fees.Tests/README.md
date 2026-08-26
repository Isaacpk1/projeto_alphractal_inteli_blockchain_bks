# Alphractal.Fees.Tests

Projeto de testes ainda não criado. Para gerar com as versões corretas de pacote:

```bash
cd api
dotnet new xunit -o tests/Alphractal.Fees.Tests
dotnet sln Alphractal.Fees.slnx add tests/Alphractal.Fees.Tests
dotnet add tests/Alphractal.Fees.Tests reference src/Alphractal.Fees.Api
```

**O que precisa de teste, em ordem de risco**

1. **Conversão de unidade.** Errar por 10⁹ é o bug mais caro e mais silencioso do projeto.
2. **RN-01 a RN-05** com `BigInteger`, incluindo os extremos (base fee mínima, bloco vazio, bloco cheio).
3. **Projeção da base fee do EIP-1559** — é determinística, então tem resposta certa e verificável.
4. **Guarda de arquitetura:** falhar se alguma query em `Repositories/Sql/` citar
   tabela fora do padrão `v_*`.
