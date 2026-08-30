namespace Alphractal.Fees.Api.Models.Responses;

/// <summary>
/// DTOs do caminho frio. Espelhados em <c>web/src/types/contract.ts</c> —
/// mudou um campo aqui, muda la NO MESMO PR (RNF-13).
/// </summary>
/// <remarks>
/// Serializados em camelCase pelo padrao do ASP.NET Core. Unidades ja
/// convertidas: gwei, ETH e USD. Wei nao sai da API.
/// </remarks>
public sealed record LatestBlockResponse
{
    public required ulong BlockNumber { get; init; }
    public required DateTimeOffset BlockTimestampUtc { get; init; }
    public required double BaseFeeGwei { get; init; }
    public required double NextBaseFeeGwei { get; init; }
    public required double PriorityFeeGwei { get; init; }
    public required ulong GasUsed { get; init; }
    public required ulong GasLimit { get; init; }
    public required double GasUsedRatio { get; init; }
    public required uint TxCount { get; init; }
    public required double BurnedEth { get; init; }
    public required decimal EthUsd { get; init; }

    /// <summary>Idade do dado em segundos — alimenta o aviso de "dado atrasado" (RN-07).</summary>
    public required double DataAgeSeconds { get; init; }

    /// <summary>
    /// Sempre <c>"cold"</c> nesta rota. O painel usa isto para nao confundir o
    /// fallback historico com o snapshot ao vivo servido da memoria (RN-14).
    /// </summary>
    public required string Source { get; init; }
}

/// <summary>
/// Amostra sub-bloco. <b>Nao e o tamanho do mempool.</b>
/// </summary>
/// <remarks>
/// <see cref="PendingBlockTxCount"/> conta as transacoes que o no ja selecionou
/// para o proximo bloco (tipicamente 100–300), nao a fila inteira de espera
/// (~10^5). O tamanho real do mempool exigiria <c>txpool_status</c>, metodo do
/// Geth que a Alchemy nao expoe.
/// <para>
/// O rotulo importa: um painel dizendo "Mempool: 114 tx" seria falso por tres
/// ordens de grandeza. Como sinal de pressao sub-bloco o dado e legitimo — e a
/// unica metrica do sistema que se move entre blocos.
/// </para>
/// </remarks>
public sealed record MempoolNowResponse
{
    public required DateTimeOffset SampledAtUtc { get; init; }
    public required ulong BlockNumber { get; init; }

    /// <summary>Transacoes no bloco pendente. Nao confundir com o tamanho do mempool.</summary>
    public required uint PendingBlockTxCount { get; init; }
    public required double BaseFeeGwei { get; init; }
    public required double PrioritySlowGwei { get; init; }
    public required double PriorityStandardGwei { get; init; }
    public required double PriorityFastGwei { get; init; }
    public required decimal EthUsd { get; init; }
}

public sealed record FeeEstimateResponse
{
    public required string Operation { get; init; }
    public required string Speed { get; init; }
    public required uint GasUnits { get; init; }
    public required decimal TotalFeeGwei { get; init; }
    public required decimal TotalFeeUsd { get; init; }
    public required DateTimeOffset LastSampledAtUtc { get; init; }
}

public sealed record FeeHistoryPointResponse
{
    public required DateTimeOffset BucketUtc { get; init; }
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

public sealed record FeeEstimateDailyResponse
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

public sealed record ComponentStatusResponse
{
    public required string Component { get; init; }
    public required string Status { get; init; }
    public required uint LagMs { get; init; }
    public required ulong LastBlock { get; init; }
    public required string Detail { get; init; }
    public required DateTimeOffset LastSeenAtUtc { get; init; }

    /// <summary>Segundos desde o ultimo heartbeat deste componente.</summary>
    public required double SecondsSinceLastSeen { get; init; }
}

/// <summary>
/// D-02 — a base fee atual contra os ultimos 30 dias.
/// </summary>
/// <remarks>
/// Complementa o indice de congestionamento, nao o substitui: aquele responde
/// "esta subindo agora?", este responde "esta caro historicamente?".
/// </remarks>
public sealed record HistoricalPositionResponse
{
    public required double CurrentBaseFeeGwei { get; init; }

    /// <summary>Posicao aproximada na distribuicao, de 0 a 100.</summary>
    public required double PercentileRank { get; init; }

    /// <summary><c>muito barato</c> … <c>muito caro</c>.</summary>
    public required string Label { get; init; }

    /// <summary>Horas de historico na janela. 720 = 30 dias completos.</summary>
    public required ulong Buckets { get; init; }

    /// <summary>
    /// Historico curto demais para afirmar posicao. O painel deve exibir o aviso
    /// junto do numero, nunca esconder um dos dois.
    /// </summary>
    public required bool LowConfidence { get; init; }

    public required DateTimeOffset FromUtc { get; init; }
    public required DateTimeOffset ToUtc { get; init; }

    /// <summary>Limiares da distribuicao, em gwei — permitem desenhar a regua no painel.</summary>
    public required IReadOnlyDictionary<string, double> ThresholdsGwei { get; init; }
}

/// <summary>Envelope das series historicas: o front precisa saber o que pediu.</summary>
public sealed record HistoryResponse<T>
{
    public required string Granularity { get; init; }
    public required DateTimeOffset FromUtc { get; init; }
    public required DateTimeOffset ToUtc { get; init; }
    public required int Count { get; init; }
    public required IReadOnlyList<T> Items { get; init; }
}
