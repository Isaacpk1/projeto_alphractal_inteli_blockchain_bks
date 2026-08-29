using Alphractal.Fees.Api.Models.Domain;
using Alphractal.Fees.Api.Models.Domain.ColdPath;
using Alphractal.Fees.Api.Models.Responses;
using Alphractal.Fees.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Alphractal.Fees.Api.Controllers;

/// <summary>
/// Transporte do caminho frio: recebe, valida, orquestra e serializa.
/// Nenhum calculo mora aqui (RN-09).
/// </summary>
/// <remarks>
/// O SSE e o snapshot ao vivo NAO ficam neste controller enquanto forem servidos
/// da janela em memoria (RN-14). Quando existirem, entram como actions proprias
/// alimentadas por <c>Services/</c>, nunca por <see cref="IFeesHistoryRepository"/>.
/// </remarks>
[ApiController]
[Route("api/v1/fees")]
[Produces("application/json")]
public sealed class FeesController : ControllerBase
{
    private const int DefaultLimit = 1_000;
    private const int MaxLimit = 10_000;

    private readonly IFeesHistoryRepository _repository;

    public FeesController(IFeesHistoryRepository repository) => _repository = repository;

    /// <summary>
    /// Ultimo bloco segundo o CAMINHO FRIO. Diagnostico e fallback de
    /// demonstracao — o painel ao vivo usa o snapshot em memoria.
    /// </summary>
    [HttpGet("latest")]
    [ProducesResponseType<LatestBlockResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<LatestBlockResponse>> GetLatest(CancellationToken cancellationToken)
    {
        var block = await _repository.GetLatestBlockAsync(cancellationToken);
        if (block is null)
        {
            return Problem(
                title: "Sem blocos carregados",
                detail: "O ClickHouse respondeu, mas nao ha bloco em v_latest_block. Rode o backfill ou o ETL.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(ToResponse(block));
    }

    /// <summary>Amostra de mempool mais recente registrada pelo caminho frio.</summary>
    [HttpGet("mempool")]
    [ProducesResponseType<MempoolNowResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<MempoolNowResponse>> GetMempool(CancellationToken cancellationToken)
    {
        var sample = await _repository.GetMempoolNowAsync(cancellationToken);
        if (sample is null)
        {
            return Problem(
                title: "Sem amostra de mempool",
                detail: "Nenhuma linha em v_mempool_now.",
                statusCode: StatusCodes.Status404NotFound);
        }

        return Ok(new MempoolNowResponse
        {
            SampledAtUtc = sample.SampledAt,
            BlockNumber = sample.BlockNumber,
            PendingTxCount = sample.PendingTxCount,
            BaseFeeGwei = sample.BaseFeeGwei,
            PrioritySlowGwei = sample.PrioritySlowGwei,
            PriorityStandardGwei = sample.PriorityStandardGwei,
            PriorityFastGwei = sample.PriorityFastGwei,
            EthUsd = sample.EthUsd,
        });
    }

    /// <summary>Ultima estimativa por operacao e velocidade, do caminho frio.</summary>
    [HttpGet("estimates")]
    [ProducesResponseType<IReadOnlyList<FeeEstimateResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<FeeEstimateResponse>>> GetEstimates(
        CancellationToken cancellationToken)
    {
        var estimates = await _repository.GetFeeEstimatesNowAsync(cancellationToken);
        return Ok(estimates.Select(item => new FeeEstimateResponse
        {
            Operation = item.Operation,
            Speed = item.Speed,
            GasUnits = item.GasUnits,
            TotalFeeGwei = item.TotalFeeGwei,
            TotalFeeUsd = item.TotalFeeUsd,
            LastSampledAtUtc = item.LastSampledAt,
        }).ToList());
    }

    /// <summary>
    /// Serie historica de taxas. <paramref name="granularity"/> aceita
    /// <c>hour</c> ou <c>day</c>; a janela padrao e de 24 h ate agora.
    /// </summary>
    [HttpGet("history")]
    [ProducesResponseType<HistoryResponse<FeeHistoryPointResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HistoryResponse<FeeHistoryPointResponse>>> GetHistory(
        CancellationToken cancellationToken,
        [FromQuery] string granularity = "hour",
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int? limit = null)
    {
        if (!TryParseGranularity(granularity, out var parsed))
        {
            return Problem(
                title: "Granularidade invalida",
                detail: "Valores aceitos: 'hour' ou 'day'.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        if (!TryResolveRange(from, to, limit, parsed, out var range, out var error))
        {
            return Problem(title: "Intervalo invalido", detail: error, statusCode: StatusCodes.Status400BadRequest);
        }

        var points = await _repository.GetFeeHistoryAsync(
            parsed, range.From, range.To, range.Limit, cancellationToken);

        return Ok(new HistoryResponse<FeeHistoryPointResponse>
        {
            Granularity = parsed == HistoryGranularity.Day ? "day" : "hour",
            FromUtc = range.From,
            ToUtc = range.To,
            Count = points.Count,
            Items = points.Select(static point => new FeeHistoryPointResponse
            {
                BucketUtc = point.Bucket,
                Blocks = point.Blocks,
                BaseFeeGweiAvg = point.BaseFeeGweiAvg,
                BaseFeeGweiMin = point.BaseFeeGweiMin,
                BaseFeeGweiMax = point.BaseFeeGweiMax,
                BaseFeeGweiP50 = point.BaseFeeGweiP50,
                BaseFeeGweiP90 = point.BaseFeeGweiP90,
                BaseFeeGweiP95 = point.BaseFeeGweiP95,
                PriorityFeeGweiAvg = point.PriorityFeeGweiAvg,
                GasUsedRatioAvg = point.GasUsedRatioAvg,
                TxCount = point.TxCount,
                BurnedEth = point.BurnedEth,
                EthUsdAvg = point.EthUsdAvg,
            }).ToList(),
        });
    }

    /// <summary>Custo diario por operacao e velocidade (D-04).</summary>
    [HttpGet("estimates/history")]
    [ProducesResponseType<HistoryResponse<FeeEstimateDailyResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<HistoryResponse<FeeEstimateDailyResponse>>> GetEstimatesHistory(
        CancellationToken cancellationToken,
        [FromQuery] DateTimeOffset? from = null,
        [FromQuery] DateTimeOffset? to = null,
        [FromQuery] int? limit = null)
    {
        if (!TryResolveRange(from, to, limit, HistoryGranularity.Day, out var range, out var error))
        {
            return Problem(title: "Intervalo invalido", detail: error, statusCode: StatusCodes.Status400BadRequest);
        }

        var rows = await _repository.GetFeeEstimatesDailyAsync(
            range.From, range.To, range.Limit, cancellationToken);

        return Ok(new HistoryResponse<FeeEstimateDailyResponse>
        {
            Granularity = "day",
            FromUtc = range.From,
            ToUtc = range.To,
            Count = rows.Count,
            Items = rows.Select(static row => new FeeEstimateDailyResponse
            {
                Bucket = row.Bucket,
                Operation = row.Operation,
                Speed = row.Speed,
                Samples = row.Samples,
                UsdAvg = row.UsdAvg,
                UsdMin = row.UsdMin,
                UsdMax = row.UsdMax,
                UsdP50 = row.UsdP50,
                UsdP90 = row.UsdP90,
            }).ToList(),
        });
    }

    private static LatestBlockResponse ToResponse(ColdLatestBlock block) => new()
    {
        BlockNumber = block.BlockNumber,
        BlockTimestampUtc = block.BlockTimestamp,
        BaseFeeGwei = block.BaseFeeGwei,
        NextBaseFeeGwei = block.NextBaseFeeGwei,
        PriorityFeeGwei = block.PriorityFeeGwei,
        GasUsed = block.GasUsed,
        GasLimit = block.GasLimit,
        GasUsedRatio = block.GasUsedRatio,
        TxCount = block.TxCount,
        BurnedEth = block.BurnedEth,
        EthUsd = block.EthUsd,
        DataAgeSeconds = Math.Max(0, block.AgeMs) / 1000d,
        Source = "cold",
    };

    private static bool TryParseGranularity(string value, out HistoryGranularity granularity)
    {
        switch (value.Trim().ToLowerInvariant())
        {
            case "hour":
            case "1h":
                granularity = HistoryGranularity.Hour;
                return true;
            case "day":
            case "1d":
                granularity = HistoryGranularity.Day;
                return true;
            default:
                granularity = HistoryGranularity.Hour;
                return false;
        }
    }

    private static bool TryResolveRange(
        DateTimeOffset? from,
        DateTimeOffset? to,
        int? limit,
        HistoryGranularity granularity,
        out (DateTimeOffset From, DateTimeOffset To, int Limit) range,
        out string error)
    {
        var end = (to ?? DateTimeOffset.UtcNow).ToUniversalTime();
        var defaultWindow = granularity == HistoryGranularity.Day
            ? TimeSpan.FromDays(30)
            : TimeSpan.FromHours(24);
        var start = (from ?? end - defaultWindow).ToUniversalTime();

        range = default;
        error = string.Empty;

        if (start >= end)
        {
            error = "'from' deve ser anterior a 'to'.";
            return false;
        }

        var resolvedLimit = limit ?? DefaultLimit;
        if (resolvedLimit is < 1 or > MaxLimit)
        {
            error = $"'limit' deve estar entre 1 e {MaxLimit}.";
            return false;
        }

        range = (start, end, resolvedLimit);
        return true;
    }
}
