using System.Numerics;

namespace Alphractal.Fees.Api.Models.Domain;

/// <summary>Faixas de velocidade de inclusao (RN-02).</summary>
public enum SpeedTier
{
    Slow = 0,
    Standard = 1,
    Fast = 2,
}

/// <summary>Rotulo do indice de congestionamento (RN-04).</summary>
public enum CongestionLevel
{
    Low = 0,
    Normal = 1,
    High = 2,
    Extreme = 3,
}

/// <summary>
/// Os tres percentis de priority fee de UM bloco, como o <c>eth_feeHistory</c>
/// devolve no array <c>reward</c>.
/// </summary>
public sealed record PriorityFeeSample
{
    public required BigInteger Slow { get; init; }
    public required BigInteger Standard { get; init; }
    public required BigInteger Fast { get; init; }

    public BigInteger For(SpeedTier tier) => tier switch
    {
        SpeedTier.Slow => Slow,
        SpeedTier.Fast => Fast,
        _ => Standard,
    };
}

/// <summary>Custo estimado de uma operacao numa faixa de velocidade (RN-01).</summary>
public sealed record FeeEstimate
{
    public required string Operation { get; init; }
    public required SpeedTier Speed { get; init; }
    public required uint GasUnits { get; init; }
    public required BigInteger BaseFeePerGas { get; init; }
    public required BigInteger PriorityFeePerGas { get; init; }

    /// <summary>Custo total em wei. Fonte de verdade — o resto e formatacao.</summary>
    public required BigInteger TotalFeeWei { get; init; }
}

/// <summary>Estado do congestionamento da rede num instante (RN-04).</summary>
public sealed record NetworkCongestion
{
    public required CongestionLevel Level { get; init; }

    /// <summary>Razao entre a base fee atual e a media movel de N_cong blocos.</summary>
    public required double Ratio { get; init; }

    public required BigInteger BaseFeePerGas { get; init; }
    public required BigInteger MovingAverage { get; init; }

    /// <summary>Blocos efetivamente usados na media. Menor que N_cong enquanto a janela enche.</summary>
    public required int SampleSize { get; init; }
}
