using System.Numerics;
using Alphractal.Fees.Api.Models.Domain;
using Alphractal.Fees.Api.Models.Responses;
using Alphractal.Fees.Api.Providers;

namespace Alphractal.Fees.Api.Services;

/// <summary>
/// Monta o payload do painel a partir do bloco, das faixas, da janela e da
/// cotacao. Toda a matematica vem de <see cref="FeeCalculator"/> (RN-09).
/// </summary>
/// <remarks>
/// Traduz enum de dominio para string em portugues no limite da API. O front
/// exibe o rotulo direto e nao replica regra nenhuma — se amanha o parceiro
/// quiser "Congestionado" no lugar de "Alto", muda aqui e so aqui.
/// </remarks>
public sealed class SnapshotBuilder
{
    private readonly FeeCalculator _calculator;

    public SnapshotBuilder(FeeCalculator calculator) => _calculator = calculator;

    public FeesSnapshotResponse Build(
        ChainBlockHeader block,
        PriorityFeeSample tiers,
        IReadOnlyList<BigInteger> recentBaseFees,
        EthPrice price,
        int windowSize,
        DateTimeOffset now)
    {
        var congestion = _calculator.Congestion(block.BaseFeePerGas, recentBaseFees);
        var nextBaseFee = FeeCalculator.NextBaseFee(block.BaseFeePerGas, block.GasUsed, block.GasLimit);
        var estimates = _calculator.EstimateAll(block.BaseFeePerGas, tiers);
        var ageSeconds = Math.Max(0, (now - block.Timestamp).TotalSeconds);

        return new FeesSnapshotResponse
        {
            BlockNumber = (ulong)block.Number,
            BlockHash = block.Hash,
            BlockTimestampUtc = block.Timestamp,
            BaseFeeGwei = FeeCalculator.ToGwei(block.BaseFeePerGas),
            NextBaseFeeGwei = FeeCalculator.ToGwei(nextBaseFee),
            GasUsed = (ulong)block.GasUsed,
            GasLimit = (ulong)block.GasLimit,
            GasUsedRatio = block.GasLimit > 0 ? (double)block.GasUsed / (double)block.GasLimit : 0,
            Congestion = new CongestionResponse
            {
                Level = Label(congestion.Level),
                Ratio = congestion.Ratio,
                MovingAverageGwei = FeeCalculator.ToGwei(congestion.MovingAverage),
                SampleSize = congestion.SampleSize,
            },
            Speeds =
            [
                new SpeedTierResponse { Speed = Label(SpeedTier.Slow), PriorityFeeGwei = FeeCalculator.ToGwei(tiers.Slow) },
                new SpeedTierResponse { Speed = Label(SpeedTier.Standard), PriorityFeeGwei = FeeCalculator.ToGwei(tiers.Standard) },
                new SpeedTierResponse { Speed = Label(SpeedTier.Fast), PriorityFeeGwei = FeeCalculator.ToGwei(tiers.Fast) },
            ],
            Estimates = estimates.Select(estimate => new OperationCostResponse
            {
                Operation = estimate.Operation,
                Speed = Label(estimate.Speed),
                GasUnits = estimate.GasUnits,
                TotalFeeGwei = FeeCalculator.ToGwei(estimate.TotalFeeWei),
                TotalFeeEth = FeeCalculator.ToEth(estimate.TotalFeeWei),
                // Sem cotacao, o campo some do JSON em vez de virar 0.00 —
                // um custo "zero dolares" na tela seria pior que campo ausente.
                TotalFeeUsd = price.HasValue
                    ? FeeCalculator.ToUsd(estimate.TotalFeeWei, price.Price)
                    : null,
            }).ToList(),
            EthUsd = price.HasValue
                ? new EthPriceResponse
                {
                    Price = price.Price,
                    ObservedAtUtc = price.ObservedAt,
                    IsStale = _calculator.IsPriceStale(price.ObservedAt, now),
                    Source = price.Source,
                }
                : null,
            DataAgeSeconds = ageSeconds,
            IsStale = _calculator.IsStale(block.Timestamp, now),
            DeliveryLatencySeconds = block.DeliveryLatency.TotalSeconds,
            WindowSize = windowSize,
            Source = "live",
        };
    }

    private static string Label(SpeedTier tier) => tier switch
    {
        SpeedTier.Slow => "lento",
        SpeedTier.Fast => "rapido",
        _ => "padrao",
    };

    private static string Label(CongestionLevel level) => level switch
    {
        CongestionLevel.Low => "baixo",
        CongestionLevel.High => "alto",
        CongestionLevel.Extreme => "extremo",
        _ => "normal",
    };
}
