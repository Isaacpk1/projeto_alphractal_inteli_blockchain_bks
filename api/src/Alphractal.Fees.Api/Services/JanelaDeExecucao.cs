using Alphractal.Fees.Api.Models.Domain.ColdPath;

namespace Alphractal.Fees.Api.Services;

/// <summary>Recomendacao de quando executar uma operacao.</summary>
public sealed record RecomendacaoDeHorario
{
    public required int MelhorHoraUtc { get; init; }
    public required double MelhorHoraGwei { get; init; }
    public required int PiorHoraUtc { get; init; }
    public required double PiorHoraGwei { get; init; }

    /// <summary>Media geral das 24 horas.</summary>
    public required double MediaGeralGwei { get; init; }

    /// <summary>
    /// Economia percentual de executar na melhor hora em vez de agora.
    /// Negativa quando o momento atual ja e melhor que a media da melhor hora.
    /// </summary>
    public required double EconomiaPercentual { get; init; }

    /// <summary>Horas ate a proxima ocorrencia da melhor hora.</summary>
    public required int HorasDeEspera { get; init; }

    /// <summary>Historico curto demais para recomendar horario.</summary>
    public required bool PoucaConfianca { get; init; }
}

/// <summary>
/// Traduz a media por hora do dia numa recomendacao acionavel.
/// </summary>
/// <remarks>
/// E a metrica que responde "o que eu faco AGORA?" — que o README define como o
/// diferencial do projeto. As outras respondem sobre o estado: esta subindo
/// (RN-04), esta caro historicamente (D-02). Nenhuma diz se vale esperar.
/// </remarks>
public static class JanelaDeExecucao
{
    /// <summary>
    /// Minimo de amostras por hora para recomendar. Abaixo disso, cada hora tem
    /// menos de tres observacoes e a "melhor hora" pode ser ruido.
    /// </summary>
    public const ulong MinimoAmostrasPorHora = 3;

    public static RecomendacaoDeHorario? Calcular(
        double gweiAgora,
        IReadOnlyList<ColdHoraDoDia> horas,
        DateTimeOffset agora)
    {
        ArgumentNullException.ThrowIfNull(horas);

        var validas = horas.Where(h => h.Amostras > 0).ToList();
        if (validas.Count == 0)
        {
            return null;
        }

        var melhor = validas.MinBy(h => h.BaseFeeGweiAvg)!;
        var pior = validas.MaxBy(h => h.BaseFeeGweiAvg)!;
        var media = validas.Average(h => h.BaseFeeGweiAvg);

        // Comparar a melhor hora com o valor DE AGORA, nao com a media do dia:
        // a pergunta do usuario e "espero ou executo?", e o ponto de comparacao
        // dele e o preco que esta vendo na tela.
        var economia = gweiAgora > 0
            ? (gweiAgora - melhor.BaseFeeGweiAvg) / gweiAgora * 100
            : 0;

        var horasDeEspera = (melhor.HoraUtc - agora.UtcDateTime.Hour + 24) % 24;

        return new RecomendacaoDeHorario
        {
            MelhorHoraUtc = melhor.HoraUtc,
            MelhorHoraGwei = melhor.BaseFeeGweiAvg,
            PiorHoraUtc = pior.HoraUtc,
            PiorHoraGwei = pior.BaseFeeGweiAvg,
            MediaGeralGwei = media,
            EconomiaPercentual = Math.Round(economia, 1),
            HorasDeEspera = horasDeEspera,
            PoucaConfianca = validas.Count < 24
                             || validas.Any(h => h.Amostras < MinimoAmostrasPorHora),
        };
    }
}
