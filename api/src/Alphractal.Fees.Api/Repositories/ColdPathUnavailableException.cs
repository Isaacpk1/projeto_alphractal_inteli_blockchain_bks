namespace Alphractal.Fees.Api.Repositories;

/// <summary>
/// O ClickHouse nao respondeu. Vira HTTP 503, nunca 500: o caminho frio fora do
/// ar e estado previsto do sistema, nao defeito da API. O painel ao vivo tem de
/// continuar funcionando sem ele (linha de corte do MVP, doc 09 secao 4).
/// </summary>
public sealed class ColdPathUnavailableException : Exception
{
    public ColdPathUnavailableException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
