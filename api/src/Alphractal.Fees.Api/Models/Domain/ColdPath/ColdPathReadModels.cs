namespace Alphractal.Fees.Api.Models.Domain.ColdPath;

/// <summary>
/// Modelos de leitura do caminho frio — o que as views <c>v_*</c> devolvem.
/// </summary>
/// <remarks>
/// Nao ha <see cref="System.Numerics.BigInteger"/> aqui porque nao ha wei: as
/// views de <c>infra/clickhouse/initdb/004_views.sql</c> ja convertem para gwei,
/// ETH e USD. Wei so aparece no caminho quente, onde a aritmetica acontece.
/// <para>
/// Estes tipos mudam quando a rede ou o banco muda. Se algo aqui esta mudando
/// porque o front pediu outro formato, o lugar certo e <c>Models/Responses/</c>.
/// </para>
/// </remarks>
public sealed record ColdLatestBlock
{
    public required ulong BlockNumber { get; init; }
    public required DateTimeOffset BlockTimestamp { get; init; }
    public required double BaseFeeGwei { get; init; }
    public required double NextBaseFeeGwei { get; init; }
    public required double PriorityFeeGwei { get; init; }
    public required ulong GasUsed { get; init; }
    public required ulong GasLimit { get; init; }
    public required double GasUsedRatio { get; init; }
    public required uint TxCount { get; init; }
    public required double BurnedEth { get; init; }
    public required decimal EthUsd { get; init; }
    public required long AgeMs { get; init; }
}

public sealed record ColdMempoolSample
{
    public required DateTimeOffset SampledAt { get; init; }
    public required ulong BlockNumber { get; init; }
    public required uint PendingTxCount { get; init; }
    public required double BaseFeeGwei { get; init; }
    public required double PrioritySlowGwei { get; init; }
    public required double PriorityStandardGwei { get; init; }
    public required double PriorityFastGwei { get; init; }
    public required decimal EthUsd { get; init; }
}

public sealed record ColdFeeEstimate
{
    public required string Operation { get; init; }
    public required string Speed { get; init; }
    public required uint GasUnits { get; init; }
    public required decimal TotalFeeGwei { get; init; }
    public required decimal TotalFeeUsd { get; init; }
    public required DateTimeOffset LastSampledAt { get; init; }
}

public sealed record ColdFeeHistoryPoint
{
    public required DateTimeOffset Bucket { get; init; }
    public required ulong Blocks { get; init; }
    public required double BaseFeeGweiAvg { get; init; }
    public required double BaseFeeGweiMin { get; init; }
    public required double BaseFeeGweiMax { get; init; }
    public required double BaseFeeGweiP50 { get; init; }
    public required double BaseFeeGweiP90 { get; init; }
    public required double BaseFeeGweiP95 { get; init; }
    public required double PriorityFeeGweiAvg { get; init; }
    public required double GasUsedRatioAvg { get; init; }
    public required ulong TxCount { get; init; }
    public required double BurnedEth { get; init; }
    public required decimal EthUsdAvg { get; init; }
}

public sealed record ColdFeeEstimateDaily
{
    public required DateOnly Bucket { get; init; }
    public required string Operation { get; init; }
    public required string Speed { get; init; }
    public required ulong Samples { get; init; }
    public required decimal UsdAvg { get; init; }
    public required decimal UsdMin { get; init; }
    public required decimal UsdMax { get; init; }
    public required decimal UsdP50 { get; init; }
    public required decimal UsdP90 { get; init; }
}

public sealed record ColdComponentHealth
{
    public required string Component { get; init; }
    public required string Status { get; init; }
    public required uint LagMs { get; init; }
    public required ulong LastBlock { get; init; }
    public required string Detail { get; init; }
    public required DateTimeOffset LastSeenAt { get; init; }
}
