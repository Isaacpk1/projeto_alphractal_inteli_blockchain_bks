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
    public required double TotalFeeEth { get; init; }
    public required decimal EthUsd { get; init; }
    public required long AgeMs { get; init; }
}

public sealed record ColdMempoolSample
{
    public required DateTimeOffset SampledAt { get; init; }
    public required ulong BlockNumber { get; init; }
    /// <summary>
    /// Transacoes no bloco pendente (coluna <c>pending_tx_count</c>), nao o
    /// tamanho do mempool. Ver <c>MempoolNowResponse</c>.
    /// </summary>
    public required uint PendingBlockTxCount { get; init; }
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
    public required double TotalFeeEth { get; init; }
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

/// <summary>
/// Distribuicao da base fee horaria nos ultimos 30 dias (D-02), em gwei.
/// </summary>
/// <remarks>
/// <see cref="Buckets"/> importa: a janela so tem 720 horas quando ha 30 dias de
/// dado. Com o sistema recem-instalado sao poucas dezenas, e um percentil sobre
/// 12 horas nao responde "esta caro historicamente?" — responde "esta caro hoje
/// de manha?". Quem consome precisa poder distinguir os dois casos.
/// </remarks>
public sealed record ColdBaseFeeDistribution
{
    public required ulong Buckets { get; init; }
    public required DateTimeOffset FromBucket { get; init; }
    public required DateTimeOffset ToBucket { get; init; }
    public required double P05Gwei { get; init; }
    public required double P10Gwei { get; init; }
    public required double P25Gwei { get; init; }
    public required double P50Gwei { get; init; }
    public required double P75Gwei { get; init; }
    public required double P90Gwei { get; init; }
    public required double P95Gwei { get; init; }
    public required double MinGwei { get; init; }
    public required double MaxGwei { get; init; }
}

/// <summary>Media da base fee numa hora do dia (UTC), sobre 30 dias.</summary>
public sealed record ColdHoraDoDia
{
    public required int HoraUtc { get; init; }
    public required ulong Amostras { get; init; }
    public required double BaseFeeGweiAvg { get; init; }
    public required double BaseFeeGweiP50 { get; init; }
    public required double BaseFeeGweiMin { get; init; }
    public required double BaseFeeGweiMax { get; init; }
}

/// <summary>Celula da grade dia-da-semana x hora. <c>DiaSemana</c>: 1 = segunda.</summary>
public sealed record ColdSemanaHora
{
    public required int DiaSemana { get; init; }
    public required int HoraUtc { get; init; }
    public required ulong Amostras { get; init; }
    public required double BaseFeeGweiAvg { get; init; }
}

/// <summary>Cotacao atual e de 24 h atras.</summary>
/// <remarks>
/// <see cref="Amostras24h"/> distingue "variou 0%" de "nao ha cotacao de 24 h
/// atras" — os dois dariam o mesmo numero e significam coisas opostas.
/// </remarks>
public sealed record ColdEthUsd24h
{
    public required decimal PrecoAtual { get; init; }
    public required DateTimeOffset ObservadoEm { get; init; }
    public required decimal Preco24h { get; init; }
    public required DateTimeOffset ObservadoEm24h { get; init; }
    public required ulong Amostras24h { get; init; }
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
