namespace Alphractal.Fees.Api.Models.Responses;

/// <summary>
/// Payload de cada evento SSE e da rota de snapshot. E o contrato do painel —
/// espelhado em <c>web/src/types/contract.ts</c>, sem compilador que valide os
/// dois lados. Mudou aqui, muda la no MESMO PR (RNF-13).
/// </summary>
/// <remarks>
/// Nenhum valor em wei sai daqui (RN-06): o front nao faz aritmetica de wei e
/// assim nao esbarra no limite de 2^53 do <c>number</c> do JavaScript.
/// </remarks>
public sealed record FeesSnapshotResponse
{
    public required ulong BlockNumber { get; init; }
    public required string BlockHash { get; init; }
    public required DateTimeOffset BlockTimestampUtc { get; init; }

    public required decimal BaseFeeGwei { get; init; }

    /// <summary>Projecao deterministica do EIP-1559 para o proximo bloco (RN-05).</summary>
    public required decimal NextBaseFeeGwei { get; init; }

    public required ulong GasUsed { get; init; }
    public required ulong GasLimit { get; init; }
    public required double GasUsedRatio { get; init; }

    public required CongestionResponse Congestion { get; init; }

    /// <summary>Faixas de velocidade (RN-02), sempre nesta ordem: lento, padrao, rapido.</summary>
    public required IReadOnlyList<SpeedTierResponse> Speeds { get; init; }

    /// <summary>Custo por operacao e velocidade (RN-01 + RN-11).</summary>
    public required IReadOnlyList<OperationCostResponse> Estimates { get; init; }

    /// <summary><c>null</c> quando nao ha cotacao — nunca zero fingindo ser preco.</summary>
    public EthPriceResponse? EthUsd { get; init; }

    /// <summary>Idade do bloco em segundos. Alimenta o aviso de "dado atrasado".</summary>
    public required double DataAgeSeconds { get; init; }

    /// <summary>Sem bloco novo ha mais de <c>Fees:StaleAfterSeconds</c> (RN-07).</summary>
    public required bool IsStale { get; init; }

    /// <summary>Segundos entre o timestamp do bloco e a chegada na API. Mede o RNF-01.</summary>
    public required double DeliveryLatencySeconds { get; init; }

    /// <summary>Blocos ja acumulados na janela de 300 (RN-10).</summary>
    public required int WindowSize { get; init; }

    /// <summary>Sempre <c>"live"</c>. O caminho frio responde <c>"cold"</c>.</summary>
    public required string Source { get; init; }
}

public sealed record CongestionResponse
{
    /// <summary><c>baixo</c>, <c>normal</c>, <c>alto</c> ou <c>extremo</c>.</summary>
    public required string Level { get; init; }

    /// <summary>Base fee atual dividida pela media movel de N_cong blocos.</summary>
    public required double Ratio { get; init; }

    public required decimal MovingAverageGwei { get; init; }

    /// <summary>Blocos usados na media. Menor que N_cong enquanto a janela enche.</summary>
    public required int SampleSize { get; init; }
}

public sealed record SpeedTierResponse
{
    /// <summary><c>lento</c>, <c>padrao</c> ou <c>rapido</c>.</summary>
    public required string Speed { get; init; }

    public required decimal PriorityFeeGwei { get; init; }
}

public sealed record OperationCostResponse
{
    public required string Operation { get; init; }
    public required string Speed { get; init; }
    public required uint GasUnits { get; init; }
    public required decimal TotalFeeGwei { get; init; }
    public required decimal TotalFeeEth { get; init; }

    /// <summary><c>null</c> quando nao ha cotacao disponivel (RN-03).</summary>
    public decimal? TotalFeeUsd { get; init; }
}

public sealed record EthPriceResponse
{
    public required decimal Price { get; init; }
    public required DateTimeOffset ObservedAtUtc { get; init; }

    /// <summary>Cotacao defasada ha mais de 5 min — o painel deve marcar como desatualizada (RN-03).</summary>
    public required bool IsStale { get; init; }

    public required string Source { get; init; }
}
