using Alphractal.Fees.Api.Models.Domain.ColdPath;
using Alphractal.Fees.Api.Services;
using Xunit;

namespace Alphractal.Fees.Tests;

/// <summary>
/// "Espero ou executo agora?" — a recomendacao de horario.
/// </summary>
public sealed class JanelaDeExecucaoTests
{
    /// <summary>24 horas com base fee subindo de 10 a 33 gwei: a hora 0 e a mais barata.</summary>
    private static List<ColdHoraDoDia> Dia(ulong amostras = 30) =>
        Enumerable.Range(0, 24).Select(hora => new ColdHoraDoDia
        {
            HoraUtc = hora,
            Amostras = amostras,
            BaseFeeGweiAvg = 10 + hora,
            BaseFeeGweiP50 = 10 + hora,
            BaseFeeGweiMin = 10 + hora,
            BaseFeeGweiMax = 10 + hora,
        }).ToList();

    private static DateTimeOffset As(int hora) =>
        new(2026, 8, 29, hora, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Encontra_a_melhor_e_a_pior_hora()
    {
        var r = JanelaDeExecucao.Calcular(20, Dia(), As(12))!;

        Assert.Equal(0, r.MelhorHoraUtc);
        Assert.Equal(10, r.MelhorHoraGwei);
        Assert.Equal(23, r.PiorHoraUtc);
        Assert.Equal(33, r.PiorHoraGwei);
    }

    [Fact]
    public void Economia_compara_com_agora_nao_com_a_media()
    {
        // O usuario decide olhando o preco na tela; comparar com a media do dia
        // responderia outra pergunta.
        var r = JanelaDeExecucao.Calcular(20, Dia(), As(12))!;

        // (20 - 10) / 20 = 50%
        Assert.Equal(50, r.EconomiaPercentual);
    }

    [Fact]
    public void Momento_ja_barato_produz_economia_negativa()
    {
        // Agora a 8 gwei, melhor hora historica a 10: esperar seria pior.
        var r = JanelaDeExecucao.Calcular(8, Dia(), As(12))!;

        Assert.True(r.EconomiaPercentual < 0);
    }

    [Theory]
    [InlineData(12, 12)] // meio-dia -> espera 12 h ate a hora 0
    [InlineData(23, 1)]  // 23h -> 1 h
    [InlineData(0, 0)]   // ja e a melhor hora
    public void Horas_de_espera_dao_a_volta_no_relogio(int horaAgora, int esperado)
    {
        var r = JanelaDeExecucao.Calcular(20, Dia(), As(horaAgora))!;

        Assert.Equal(esperado, r.HorasDeEspera);
    }

    [Fact]
    public void Poucas_amostras_marcam_baixa_confianca()
    {
        // Com 2 observacoes por hora, a "melhor hora" pode ser ruido.
        var r = JanelaDeExecucao.Calcular(20, Dia(amostras: 2), As(12))!;

        Assert.True(r.PoucaConfianca);
    }

    [Fact]
    public void Dia_incompleto_marca_baixa_confianca()
    {
        var parcial = Dia().Take(9).ToList();

        var r = JanelaDeExecucao.Calcular(20, parcial, As(12))!;

        Assert.True(r.PoucaConfianca);
    }

    [Fact]
    public void Sem_amostra_nenhuma_devolve_null_em_vez_de_recomendar()
    {
        var vazio = Dia().Select(h => h with { Amostras = 0 }).ToList();

        Assert.Null(JanelaDeExecucao.Calcular(20, vazio, As(12)));
        Assert.Null(JanelaDeExecucao.Calcular(20, [], As(12)));
    }

    [Fact]
    public void Base_fee_atual_zero_nao_divide_por_zero()
    {
        var r = JanelaDeExecucao.Calcular(0, Dia(), As(12))!;

        Assert.Equal(0, r.EconomiaPercentual);
    }
}
