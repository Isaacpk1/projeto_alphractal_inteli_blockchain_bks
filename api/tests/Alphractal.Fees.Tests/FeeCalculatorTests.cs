using System.Numerics;
using Alphractal.Fees.Api.Configuration;
using Alphractal.Fees.Api.Models.Domain;
using Alphractal.Fees.Api.Services;
using Microsoft.Extensions.Options;
using Xunit;

namespace Alphractal.Fees.Tests;

/// <summary>
/// RN-01 a RN-05. Nenhum destes testes toca rede, banco ou relogio real — e o
/// que permite escrever a matematica antes de existir chave de RPC.
/// </summary>
public sealed class FeeCalculatorTests
{
    private static FeeCalculator Build(Action<FeesOptions>? _ = null)
        => new(Options.Create(new FeesOptions { SpoolPath = "../spool" }));

    private static BigInteger Gwei(double value) => new(value * 1e9);

    // ── RN-01 ──────────────────────────────────────────────────────────────

    [Fact]
    public void Rn01_custo_e_base_mais_priority_vezes_gas()
    {
        // 30 gwei base + 2 gwei priority, transferencia simples (21.000 gas)
        var cost = FeeCalculator.TransactionCostWei(Gwei(30), Gwei(2), 21_000);

        Assert.Equal(BigInteger.Parse("672000000000000"), cost); // 0,000672 ETH
        Assert.Equal(0.000672m, FeeCalculator.ToEth(cost));
    }

    [Fact]
    public void Rn01_valor_alem_do_alcance_de_double_permanece_exato()
    {
        // O ponto do BigInteger (R-03): 1000 gwei (= 1e12 wei) x 30.000.001 de gas
        // da 3,0000001e19 — mais de mil vezes acima de 2^53 (~9,0e15), onde o
        // `number` do JavaScript ja perderia digitos. O "1" no meio do numero e
        // justamente o que sumiria: e ele que prova que nada foi arredondado.
        var cost = FeeCalculator.TransactionCostWei(Gwei(1000), Gwei(0), 30_000_001);

        Assert.Equal(BigInteger.Parse("30000001000000000000"), cost);
        Assert.True(cost > new BigInteger(9_007_199_254_740_992d), "abaixo de 2^53: o teste perde o proposito");
    }

    [Fact]
    public void Rn01_gas_ou_taxa_negativa_e_rejeitada()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => FeeCalculator.TransactionCostWei(BigInteger.MinusOne, 0, 21_000));
    }

    // ── RN-02 ──────────────────────────────────────────────────────────────

    [Fact]
    public void Rn02_faixas_usam_mediana_e_ignoram_outlier_de_um_bloco()
    {
        var normal = new PriorityFeeSample { Slow = Gwei(1), Standard = Gwei(2), Fast = Gwei(3) };
        var mev = new PriorityFeeSample { Slow = Gwei(1), Standard = Gwei(2), Fast = Gwei(500) };
        var samples = new[] { normal, normal, mev, normal, normal };

        var tiers = Build().SpeedTiers(samples);

        // A media de Fast daria ~102 gwei por causa de um unico bloco.
        Assert.Equal(Gwei(3), tiers.Fast);
    }

    [Fact]
    public void Rn02_faixas_sao_crescentes_e_respeitam_a_janela_n_fee()
    {
        // 30 blocos, mas N_fee = 20: so os 20 mais recentes contam.
        var antigos = Enumerable.Repeat(
            new PriorityFeeSample { Slow = Gwei(100), Standard = Gwei(200), Fast = Gwei(300) }, 10);
        var recentes = Enumerable.Repeat(
            new PriorityFeeSample { Slow = Gwei(1), Standard = Gwei(2), Fast = Gwei(3) }, 20);

        var tiers = Build().SpeedTiers(antigos.Concat(recentes).ToList());

        Assert.Equal(Gwei(1), tiers.Slow);
        Assert.True(tiers.Slow < tiers.Standard && tiers.Standard < tiers.Fast);
    }

    [Fact]
    public void Rn02_sem_amostra_falha_em_vez_de_inventar_faixa()
    {
        Assert.Throws<ArgumentException>(() => Build().SpeedTiers(Array.Empty<PriorityFeeSample>()));
    }

    // ── RN-03 ──────────────────────────────────────────────────────────────

    [Fact]
    public void Rn03_conversao_para_usd()
    {
        var cost = FeeCalculator.TransactionCostWei(Gwei(30), Gwei(2), 21_000);

        // 0,000672 ETH x 3.200 USD
        Assert.Equal(2.1504m, FeeCalculator.ToUsd(cost, 3200m));
    }

    [Fact]
    public void Rn03_cotacao_com_mais_de_cinco_minutos_e_considerada_velha()
    {
        var calculator = Build();
        var now = DateTimeOffset.UnixEpoch.AddHours(10);

        Assert.False(calculator.IsPriceStale(now.AddMinutes(-4), now));
        Assert.True(calculator.IsPriceStale(now.AddMinutes(-6), now));
    }

    // ── RN-04 ──────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(50, CongestionLevel.Low)]      // 0,5x — bem abaixo
    [InlineData(100, CongestionLevel.Normal)]  // 1,0x
    [InlineData(129, CongestionLevel.Normal)]  // 1,29x — ainda normal
    [InlineData(130, CongestionLevel.High)]    // 1,30x — limite inferior de "alto"
    [InlineData(199, CongestionLevel.High)]
    [InlineData(200, CongestionLevel.Extreme)] // 2,00x
    public void Rn04_faixas_de_congestionamento(int currentGwei, CongestionLevel expected)
    {
        var window = Enumerable.Repeat(Gwei(100), 100).ToList();

        var result = Build().Congestion(Gwei(currentGwei), window);

        Assert.Equal(expected, result.Level);
        Assert.Equal(100, result.SampleSize);
    }

    [Fact]
    public void Rn04_janela_vazia_devolve_normal_em_vez_de_dividir_por_zero()
    {
        var result = Build().Congestion(Gwei(42), Array.Empty<BigInteger>());

        Assert.Equal(CongestionLevel.Normal, result.Level);
        Assert.Equal(0, result.SampleSize);
    }

    [Fact]
    public void Rn04_ponto_cego_conhecido_taxa_alta_sustentada_marca_normal()
    {
        // Documenta a limitacao declarada na RN-04: a regra mede VARIACAO, nao
        // NIVEL. Gas historicamente caro, mas estavel, aparece como "Normal".
        // O D-02 (percentil de 30 dias) existe para cobrir isto.
        var window = Enumerable.Repeat(Gwei(300), 100).ToList();

        var result = Build().Congestion(Gwei(300), window);

        Assert.Equal(CongestionLevel.Normal, result.Level);
    }

    // ── RN-05 ──────────────────────────────────────────────────────────────

    [Fact]
    public void Rn05_bloco_no_alvo_mantem_a_base_fee()
    {
        Assert.Equal(Gwei(100), FeeCalculator.NextBaseFee(Gwei(100), 15_000_000, 30_000_000));
    }

    [Fact]
    public void Rn05_bloco_cheio_sobe_exatamente_12_5_por_cento()
    {
        // Teto da regra: gasUsed = gasLimit e o unico caso que atinge +12,5%.
        var next = FeeCalculator.NextBaseFee(Gwei(100), 30_000_000, 30_000_000);

        Assert.Equal(Gwei(112.5), next);
    }

    [Fact]
    public void Rn05_bloco_vazio_cai_exatamente_12_5_por_cento()
    {
        var next = FeeCalculator.NextBaseFee(Gwei(100), 0, 30_000_000);

        Assert.Equal(Gwei(87.5), next);
    }

    [Fact]
    public void Rn05_variacao_e_proporcional_nao_e_sempre_o_teto()
    {
        // 3/4 do limite = meio caminho entre alvo e cheio -> metade de 12,5%.
        var next = FeeCalculator.NextBaseFee(Gwei(100), 22_500_000, 30_000_000);

        Assert.Equal(Gwei(106.25), next);
    }

    [Fact]
    public void Rn05_base_fee_baixa_sobe_pelo_menos_um_wei()
    {
        // Com base fee de 1 wei o delta arredondaria para zero e a taxa jamais
        // reagiria a congestao. O protocolo garante o incremento minimo.
        var next = FeeCalculator.NextBaseFee(BigInteger.One, 30_000_000, 30_000_000);

        Assert.Equal(new BigInteger(2), next);
    }

    [Fact]
    public void Rn05_gas_limit_zero_nao_estoura()
    {
        Assert.Equal(Gwei(100), FeeCalculator.NextBaseFee(Gwei(100), 0, 0));
    }

    // ── RN-07 e RN-11 ──────────────────────────────────────────────────────

    [Fact]
    public void Rn07_sem_bloco_novo_ha_mais_de_60s_o_dado_e_obsoleto()
    {
        var calculator = Build();
        var now = DateTimeOffset.UnixEpoch.AddHours(10);

        Assert.False(calculator.IsStale(now.AddSeconds(-59), now));
        Assert.True(calculator.IsStale(now.AddSeconds(-61), now));
    }

    [Fact]
    public void Rn11_estimativa_cobre_todas_as_operacoes_nas_tres_faixas()
    {
        var tiers = new PriorityFeeSample { Slow = Gwei(1), Standard = Gwei(2), Fast = Gwei(3) };

        var estimates = Build().EstimateAll(Gwei(30), tiers);

        Assert.Equal(15, estimates.Count); // 5 operacoes x 3 faixas
        var transfer = estimates.Single(e => e.Operation == "transfer" && e.Speed == SpeedTier.Standard);
        Assert.Equal(21_000u, transfer.GasUnits);
        Assert.Equal(FeeCalculator.TransactionCostWei(Gwei(30), Gwei(2), 21_000), transfer.TotalFeeWei);
    }

    [Fact]
    public void Rn11_mais_rapido_nunca_custa_menos_que_mais_lento()
    {
        var tiers = new PriorityFeeSample { Slow = Gwei(1), Standard = Gwei(2), Fast = Gwei(3) };

        var estimates = Build().EstimateAll(Gwei(30), tiers);

        foreach (var group in estimates.GroupBy(e => e.Operation))
        {
            var slow = group.Single(e => e.Speed == SpeedTier.Slow).TotalFeeWei;
            var standard = group.Single(e => e.Speed == SpeedTier.Standard).TotalFeeWei;
            var fast = group.Single(e => e.Speed == SpeedTier.Fast).TotalFeeWei;

            Assert.True(slow <= standard && standard <= fast, $"ordem quebrada em {group.Key}");
        }
    }
}
