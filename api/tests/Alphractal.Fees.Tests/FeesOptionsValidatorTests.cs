using Alphractal.Fees.Api.Configuration;
using Xunit;

namespace Alphractal.Fees.Tests;

/// <summary>
/// Configuracao incoerente tem de derrubar a partida, nao produzir numero errado
/// em silencio.
/// </summary>
public sealed class FeesOptionsValidatorTests
{
    private static FeesOptions Valid() => new() { SpoolPath = "../spool" };

    [Fact]
    public void Configuracao_padrao_e_valida()
    {
        Assert.True(new FeesOptionsValidator().Validate(null, Valid()).Succeeded);
    }

    [Fact]
    public void Rn10_janela_de_congestionamento_maior_que_o_buffer_e_rejeitada()
    {
        var options = new FeesOptions
        {
            SpoolPath = "../spool",
            HotWindowBlocks = 50,
            CongestionWindowBlocks = 100,
        };

        var result = new FeesOptionsValidator().Validate(null, options);

        Assert.True(result.Failed);
        Assert.Contains(result.Failures!, message => message.Contains("CongestionWindowBlocks", StringComparison.Ordinal));
    }

    [Fact]
    public void Percentis_fora_de_ordem_sao_rejeitados()
    {
        var options = new FeesOptions
        {
            SpoolPath = "../spool",
            Percentiles = new FeePercentiles { Slow = 90, Standard = 50, Fast = 10 },
        };

        Assert.True(new FeesOptionsValidator().Validate(null, options).Failed);
    }

    [Fact]
    public void Cotacao_que_nasce_vencida_e_rejeitada()
    {
        var options = new FeesOptions
        {
            SpoolPath = "../spool",
            PriceRefreshSeconds = 300,
            PriceStaleAfterSeconds = 60,
        };

        Assert.True(new FeesOptionsValidator().Validate(null, options).Failed);
    }
}
