using Alphractal.Fees.Api.Models.Domain.ColdPath;
using Alphractal.Fees.Api.Services;
using Xunit;

namespace Alphractal.Fees.Tests;

/// <summary>
/// D-02 — posicionamento da base fee atual na distribuicao de 30 dias.
/// </summary>
public sealed class HistoricalContextTests
{
    /// <summary>Distribuicao linear de 0 a 100 gwei: o percentil e o proprio valor.</summary>
    private static ColdBaseFeeDistribution Linear(ulong buckets = 720) => new()
    {
        Buckets = buckets,
        FromBucket = DateTimeOffset.UnixEpoch,
        ToBucket = DateTimeOffset.UnixEpoch.AddDays(30),
        MinGwei = 0,
        P05Gwei = 5,
        P10Gwei = 10,
        P25Gwei = 25,
        P50Gwei = 50,
        P75Gwei = 75,
        P90Gwei = 90,
        P95Gwei = 95,
        MaxGwei = 100,
    };

    [Theory]
    [InlineData(50, 50)]
    [InlineData(25, 25)]
    [InlineData(90, 90)]
    [InlineData(17.5, 17.5)] // entre p10 e p25 — exercita a interpolacao
    [InlineData(62.5, 62.5)] // entre p50 e p75
    public void Interpola_entre_os_limiares(double currentGwei, double expectedRank)
    {
        Assert.Equal(expectedRank, HistoricalContext.Rank(currentGwei, Linear()), precision: 4);
    }

    [Fact]
    public void Satura_nos_extremos_em_vez_de_extrapolar()
    {
        // Extrapolar produziria "percentil 130", que nao existe.
        Assert.Equal(0, HistoricalContext.Rank(-5, Linear()));
        Assert.Equal(100, HistoricalContext.Rank(500, Linear()));
    }

    [Fact]
    public void Distribuicao_totalmente_plana_nao_divide_por_zero()
    {
        // Rede parada por 30 dias: todos os limiares iguais.
        var flat = Linear() with
        {
            MinGwei = 1, P05Gwei = 1, P10Gwei = 1, P25Gwei = 1, P50Gwei = 1,
            P75Gwei = 1, P90Gwei = 1, P95Gwei = 1, MaxGwei = 1,
        };

        var rank = HistoricalContext.Rank(1, flat);

        Assert.InRange(rank, 0, 100);
    }

    [Theory]
    [InlineData(2, "muito barato")]
    [InlineData(15, "barato")]
    [InlineData(50, "normal")]
    [InlineData(80, "caro")]
    [InlineData(97, "muito caro")]
    public void Rotulo_acompanha_a_faixa(double currentGwei, string expected)
    {
        Assert.Equal(expected, HistoricalContext.Position(currentGwei, Linear()).Label);
    }

    [Fact]
    public void Janela_curta_e_marcada_como_pouco_confiavel()
    {
        // 12 horas de historico nao respondem "esta caro historicamente?" —
        // respondem "esta caro hoje de manha?". O painel precisa saber a diferenca.
        var curta = HistoricalContext.Position(50, Linear(buckets: 12));
        var completa = HistoricalContext.Position(50, Linear(buckets: 720));

        Assert.True(curta.LowConfidence);
        Assert.False(completa.LowConfidence);
        Assert.Equal(12ul, curta.Buckets);
    }

    [Fact]
    public void Cobre_o_ponto_cego_da_rn04()
    {
        // O caso que a RN-04 erra: taxa alta SUSTENTADA. A media movel acompanha
        // e o congestionamento marca "normal"; o percentil historico mostra que
        // o nivel esta no topo dos 30 dias. Os dois juntos contam a historia.
        var position = HistoricalContext.Position(96, Linear());

        Assert.Equal("muito caro", position.Label);
        Assert.True(position.PercentileRank > 90);
    }
}
