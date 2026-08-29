using System.Globalization;
using System.Numerics;
using Alphractal.Fees.Api.Configuration;
using Alphractal.Fees.Api.Models.Domain;
using Microsoft.Extensions.Options;

namespace Alphractal.Fees.Api.Services;

/// <summary>
/// Toda a matematica do modulo: RN-01 a RN-05 (RN-09).
/// </summary>
/// <remarks>
/// Sem estado, sem I/O, sem HTTP e sem ClickHouse — recebe e devolve tipos de
/// <c>Models/</c>. E o que torna esta classe testavel sem rede, e e por isso que
/// ela pode ser escrita e validada antes de a chave RPC existir.
/// <para>
/// Toda aritmetica de wei e <see cref="BigInteger"/> (RN-06). A conversao para
/// decimal acontece so na formatacao final, nunca no meio da conta.
/// </para>
/// </remarks>
public sealed class FeeCalculator
{
    private static readonly BigInteger WeiPerGwei = BigInteger.Pow(10, 9);
    private static readonly BigInteger WeiPerEth = BigInteger.Pow(10, 18);

    private readonly FeesOptions _options;

    public FeeCalculator(IOptions<FeesOptions> options) => _options = options.Value;

    // ── RN-01 — custo da transacao ─────────────────────────────────────────

    /// <summary>
    /// <c>custo_wei = (baseFeePerGas + priorityFee) × gasLimit</c>.
    /// </summary>
    /// <remarks>
    /// A base fee e queimada pelo protocolo; a priority fee e a unica parte que o
    /// usuario controla e e o que define a velocidade de inclusao.
    /// </remarks>
    public static BigInteger TransactionCostWei(
        BigInteger baseFeePerGas,
        BigInteger priorityFeePerGas,
        BigInteger gasUnits)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(baseFeePerGas.Sign);
        ArgumentOutOfRangeException.ThrowIfNegative(priorityFeePerGas.Sign);
        ArgumentOutOfRangeException.ThrowIfNegative(gasUnits.Sign);

        return (baseFeePerGas + priorityFeePerGas) * gasUnits;
    }

    /// <summary>Estimativas para todas as operacoes de <c>Fees:GasLimits</c> e as tres faixas.</summary>
    public IReadOnlyList<FeeEstimate> EstimateAll(BigInteger baseFeePerGas, PriorityFeeSample tiers)
    {
        var estimates = new List<FeeEstimate>(_options.GasLimits.Count * 3);

        foreach (var (operation, gasUnits) in _options.GasLimits.OrderBy(pair => pair.Key, StringComparer.Ordinal))
        {
            foreach (var speed in new[] { SpeedTier.Slow, SpeedTier.Standard, SpeedTier.Fast })
            {
                var priority = tiers.For(speed);
                estimates.Add(new FeeEstimate
                {
                    Operation = operation,
                    Speed = speed,
                    GasUnits = gasUnits,
                    BaseFeePerGas = baseFeePerGas,
                    PriorityFeePerGas = priority,
                    TotalFeeWei = TransactionCostWei(baseFeePerGas, priority, gasUnits),
                });
            }
        }

        return estimates;
    }

    // ── RN-02 — faixas de velocidade ───────────────────────────────────────

    /// <summary>
    /// Consolida os percentis de priority fee dos ultimos <c>N_fee</c> blocos numa
    /// unica faixa por velocidade, usando a MEDIANA de cada coluna.
    /// </summary>
    /// <remarks>
    /// Mediana e nao media: um unico bloco com uma gorjeta absurda (bot de MEV,
    /// liquidacao) desloca a media e faria o painel sugerir um valor caro que
    /// ninguem precisa pagar. A mediana ignora o outlier.
    /// </remarks>
    public PriorityFeeSample SpeedTiers(IReadOnlyList<PriorityFeeSample> samples)
    {
        ArgumentNullException.ThrowIfNull(samples);
        if (samples.Count == 0)
        {
            throw new ArgumentException("Sem amostras de priority fee.", nameof(samples));
        }

        // Só os N_fee mais recentes entram, mesmo que a janela quente tenha mais.
        var window = samples.Count <= _options.FeeWindowBlocks
            ? samples
            : samples.Skip(samples.Count - _options.FeeWindowBlocks).ToList();

        return new PriorityFeeSample
        {
            Slow = Median(window.Select(sample => sample.Slow)),
            Standard = Median(window.Select(sample => sample.Standard)),
            Fast = Median(window.Select(sample => sample.Fast)),
        };
    }

    // ── RN-03 — conversao para USD ─────────────────────────────────────────

    /// <summary>Converte wei em USD. A divisao acontece so aqui, no fim.</summary>
    public static decimal ToUsd(BigInteger wei, decimal ethUsd)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(ethUsd);
        return ToEth(wei) * ethUsd;
    }

    /// <summary>Converte wei em ETH como <see cref="decimal"/> (28 digitos, suficiente).</summary>
    public static decimal ToEth(BigInteger wei)
    {
        var (quotient, remainder) = BigInteger.DivRem(wei, WeiPerEth);
        return (decimal)quotient + (decimal)remainder / (decimal)WeiPerEth;
    }

    /// <summary>Converte wei em gwei como <see cref="decimal"/>.</summary>
    public static decimal ToGwei(BigInteger wei)
    {
        var (quotient, remainder) = BigInteger.DivRem(wei, WeiPerGwei);
        return (decimal)quotient + (decimal)remainder / (decimal)WeiPerGwei;
    }

    /// <summary>A cotacao esta velha demais para exibir USD como atual? (RN-03)</summary>
    public bool IsPriceStale(DateTimeOffset observedAt, DateTimeOffset now)
        => (now - observedAt).TotalSeconds > _options.PriceStaleAfterSeconds;

    // ── RN-04 — indice de congestionamento ─────────────────────────────────

    /// <summary>
    /// Compara a base fee atual com a media movel dos ultimos <c>N_cong</c> blocos.
    /// </summary>
    public NetworkCongestion Congestion(BigInteger currentBaseFee, IReadOnlyList<BigInteger> recentBaseFees)
    {
        ArgumentNullException.ThrowIfNull(recentBaseFees);

        var window = recentBaseFees.Count <= _options.CongestionWindowBlocks
            ? recentBaseFees
            : recentBaseFees.Skip(recentBaseFees.Count - _options.CongestionWindowBlocks).ToList();

        if (window.Count == 0)
        {
            // Sem historico ainda: "Normal" com razao 1 e o unico palpite honesto.
            return new NetworkCongestion
            {
                Level = CongestionLevel.Normal,
                Ratio = 1,
                BaseFeePerGas = currentBaseFee,
                MovingAverage = currentBaseFee,
                SampleSize = 0,
            };
        }

        var sum = window.Aggregate(BigInteger.Zero, static (total, value) => total + value);
        var average = sum / window.Count;

        // Media zero so acontece em rede de teste vazia; evita divisao por zero.
        var ratio = average.IsZero ? 1d : (double)currentBaseFee / (double)average;

        var thresholds = _options.Congestion;
        var level = ratio switch
        {
            _ when ratio >= thresholds.Extreme => CongestionLevel.Extreme,
            _ when ratio >= thresholds.High => CongestionLevel.High,
            _ when ratio < thresholds.Low => CongestionLevel.Low,
            _ => CongestionLevel.Normal,
        };

        return new NetworkCongestion
        {
            Level = level,
            Ratio = ratio,
            BaseFeePerGas = currentBaseFee,
            MovingAverage = average,
            SampleSize = window.Count,
        };
    }

    // ── RN-05 — projecao da base fee do proximo bloco ──────────────────────

    /// <summary>
    /// Base fee do proximo bloco pela regra deterministica do EIP-1559.
    /// </summary>
    /// <remarks>
    /// Nao e previsao estatistica: e a formula do protocolo, e o valor e exato.
    /// <para>
    /// A RN-05 descreve a regra como "sobe no maximo +12,5%". Os 12,5% sao o
    /// LIMITE (atingido quando o bloco usa todo o gas), nao a formula. O calculo
    /// real e proporcional ao quanto o gas usado se afasta do alvo:
    /// <c>delta = baseFee × (gasUsed − alvo) / alvo / 8</c>, com o incremento
    /// minimo de 1 wei quando o resultado arredondaria para zero.
    /// </para>
    /// </remarks>
    public static BigInteger NextBaseFee(BigInteger baseFeePerGas, BigInteger gasUsed, BigInteger gasLimit)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(baseFeePerGas.Sign);
        ArgumentOutOfRangeException.ThrowIfNegative(gasUsed.Sign);

        var target = gasLimit / 2;
        if (target.IsZero)
        {
            return baseFeePerGas;
        }

        if (gasUsed == target)
        {
            return baseFeePerGas;
        }

        if (gasUsed > target)
        {
            var delta = baseFeePerGas * (gasUsed - target) / target / 8;
            // O protocolo garante subida de pelo menos 1 wei quando o bloco passa
            // do alvo — sem isso, base fees baixas nunca reagiriam a congestao.
            return baseFeePerGas + BigInteger.Max(delta, BigInteger.One);
        }

        var decrease = baseFeePerGas * (target - gasUsed) / target / 8;
        return BigInteger.Max(baseFeePerGas - decrease, BigInteger.Zero);
    }

    // ── RN-07 — dado obsoleto ──────────────────────────────────────────────

    public bool IsStale(DateTimeOffset blockTimestamp, DateTimeOffset now)
        => (now - blockTimestamp).TotalSeconds > _options.StaleAfterSeconds;

    // ── auxiliares ─────────────────────────────────────────────────────────

    /// <summary>Mediana inteira: em contagem par, a media dos dois centrais.</summary>
    internal static BigInteger Median(IEnumerable<BigInteger> values)
    {
        var ordered = values.OrderBy(static value => value).ToList();
        if (ordered.Count == 0)
        {
            throw new ArgumentException("Sequencia vazia.", nameof(values));
        }

        var middle = ordered.Count / 2;
        return ordered.Count % 2 == 1
            ? ordered[middle]
            : (ordered[middle - 1] + ordered[middle]) / 2;
    }

    /// <summary>Formatacao final para exibicao (RN-06): gwei com 2 casas.</summary>
    public static string FormatGwei(BigInteger wei)
        => ToGwei(wei).ToString("0.##", CultureInfo.InvariantCulture);
}
