using Microsoft.Extensions.Options;

namespace Alphractal.Fees.Api.Configuration;

/// <summary>
/// Validacoes de <see cref="FeesOptions"/> que dependem de mais de um campo —
/// DataAnnotations so olha um campo por vez.
/// </summary>
/// <remarks>
/// A RN-10 diz que as tres janelas estao CONTIDAS no buffer de 300 blocos. Se
/// alguem configurar <c>N_cong</c> maior que <c>N_buffer</c>, a media movel
/// passaria a usar menos blocos do que pede a configuracao — silenciosamente, e
/// o painel mostraria um congestionamento errado sem nenhum erro. Falhar na
/// partida e melhor que acertar por acidente.
/// </remarks>
public sealed class FeesOptionsValidator : IValidateOptions<FeesOptions>
{
    public ValidateOptionsResult Validate(string? name, FeesOptions options)
    {
        var failures = new List<string>();

        if (options.FeeWindowBlocks > options.HotWindowBlocks)
        {
            failures.Add(
                $"Fees:FeeWindowBlocks ({options.FeeWindowBlocks}) nao pode exceder " +
                $"Fees:HotWindowBlocks ({options.HotWindowBlocks}) — RN-10.");
        }

        if (options.CongestionWindowBlocks > options.HotWindowBlocks)
        {
            failures.Add(
                $"Fees:CongestionWindowBlocks ({options.CongestionWindowBlocks}) nao pode exceder " +
                $"Fees:HotWindowBlocks ({options.HotWindowBlocks}) — RN-10.");
        }

        var percentiles = options.Percentiles;
        if (!(percentiles.Slow < percentiles.Standard && percentiles.Standard < percentiles.Fast))
        {
            failures.Add(
                "Fees:Percentiles deve ser estritamente crescente (Slow < Standard < Fast) — " +
                "caso contrario 'rapido' poderia custar menos que 'lento'.");
        }

        var congestion = options.Congestion;
        if (!(congestion.Low < congestion.High && congestion.High < congestion.Extreme))
        {
            failures.Add(
                "Fees:Congestion deve ser estritamente crescente (Low < High < Extreme) — RN-04.");
        }

        if (options.PriceStaleAfterSeconds <= options.PriceRefreshSeconds)
        {
            failures.Add(
                "Fees:PriceStaleAfterSeconds deve ser maior que Fees:PriceRefreshSeconds — " +
                "senao a cotacao nasce vencida a cada atualizacao.");
        }

        if (options.GasLimits.Count == 0)
        {
            failures.Add("Fees:GasLimits nao pode ser vazio — RN-11.");
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
