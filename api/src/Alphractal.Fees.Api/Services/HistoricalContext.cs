using Alphractal.Fees.Api.Models.Domain.ColdPath;

namespace Alphractal.Fees.Api.Services;

/// <summary>Onde a base fee atual cai na distribuicao dos ultimos 30 dias (D-02).</summary>
public sealed record HistoricalPosition
{
    /// <summary>Posicao aproximada, de 0 a 100.</summary>
    public required double PercentileRank { get; init; }

    /// <summary><c>muito barato</c>, <c>barato</c>, <c>normal</c>, <c>caro</c>, <c>muito caro</c>.</summary>
    public required string Label { get; init; }

    /// <summary>Horas de historico usadas. 720 = 30 dias completos.</summary>
    public required ulong Buckets { get; init; }

    /// <summary>
    /// Amostra insuficiente para afirmar posicao historica.
    /// </summary>
    /// <remarks>
    /// O painel deve exibir o aviso, nao esconder o numero: "p12 sobre 9 h de
    /// historico" e informacao util; "p12" sem contexto, apresentado como se
    /// fossem 30 dias, e afirmacao falsa.
    /// </remarks>
    public required bool LowConfidence { get; init; }
}

/// <summary>
/// D-02 — posiciona a base fee atual contra a distribuicao historica.
/// </summary>
/// <remarks>
/// Existe porque a RN-04 tem um ponto cego declarado: ela compara com uma media
/// movel de 100 blocos, entao mede VARIACAO e nao NIVEL. Num periodo sustentado
/// de taxas altas a media acompanha a subida e o indicador volta a marcar
/// "Normal". As duas metricas sao complementares — uma responde "esta subindo
/// agora?", esta responde "esta caro em termos historicos?".
/// </remarks>
public static class HistoricalContext
{
    /// <summary>Abaixo disto, a janela e curta demais para falar em "historico".</summary>
    public const ulong MinimumBucketsForConfidence = 168; // 7 dias

    public static HistoricalPosition Position(double currentGwei, ColdBaseFeeDistribution distribution)
    {
        ArgumentNullException.ThrowIfNull(distribution);

        var rank = Rank(currentGwei, distribution);

        return new HistoricalPosition
        {
            PercentileRank = rank,
            Label = Label(rank),
            Buckets = distribution.Buckets,
            LowConfidence = distribution.Buckets < MinimumBucketsForConfidence,
        };
    }

    /// <summary>
    /// Percentil aproximado por interpolacao linear entre os limiares conhecidos.
    /// </summary>
    /// <remarks>
    /// Aproximado de proposito: guardamos sete limiares, nao a distribuicao
    /// inteira. Trazer 720 valores por requisicao para calcular um percentil
    /// exato seria desperdicio — a diferenca nao muda o rotulo exibido.
    /// <para>
    /// Fora dos extremos o valor satura em 0 ou 100 em vez de extrapolar:
    /// extrapolar produziria numeros como "percentil 130", que nao existe.
    /// </para>
    /// </remarks>
    internal static double Rank(double currentGwei, ColdBaseFeeDistribution distribution)
    {
        // Ordenados por construcao — sao quantis da mesma distribuicao.
        var points = new (double Percentile, double Gwei)[]
        {
            (0, distribution.MinGwei),
            (5, distribution.P05Gwei),
            (10, distribution.P10Gwei),
            (25, distribution.P25Gwei),
            (50, distribution.P50Gwei),
            (75, distribution.P75Gwei),
            (90, distribution.P90Gwei),
            (95, distribution.P95Gwei),
            (100, distribution.MaxGwei),
        };

        if (currentGwei <= points[0].Gwei)
        {
            return 0;
        }

        for (var i = 1; i < points.Length; i++)
        {
            var (upperPercentile, upperGwei) = points[i];
            if (currentGwei > upperGwei)
            {
                continue;
            }

            var (lowerPercentile, lowerGwei) = points[i - 1];
            var span = upperGwei - lowerGwei;

            // Rede parada por 30 dias deixa os limiares iguais; sem esta guarda
            // seria divisao por zero.
            if (span <= 0)
            {
                return upperPercentile;
            }

            var fraction = (currentGwei - lowerGwei) / span;
            return lowerPercentile + fraction * (upperPercentile - lowerPercentile);
        }

        return 100;
    }

    private static string Label(double rank) => rank switch
    {
        < 10 => "muito barato",
        < 25 => "barato",
        <= 75 => "normal",
        <= 90 => "caro",
        _ => "muito caro",
    };
}
