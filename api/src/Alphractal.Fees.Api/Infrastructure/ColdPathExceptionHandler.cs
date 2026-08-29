using Alphractal.Fees.Api.Repositories;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Alphractal.Fees.Api.Infrastructure;

/// <summary>
/// Traduz <see cref="ColdPathUnavailableException"/> em HTTP 503 com
/// ProblemDetails. Sem isso, ClickHouse fora do ar viraria 500 e o painel
/// trataria indisponibilidade prevista como bug da API.
/// </summary>
public sealed class ColdPathExceptionHandler : IExceptionHandler
{
    private readonly ILogger<ColdPathExceptionHandler> _logger;

    public ColdPathExceptionHandler(ILogger<ColdPathExceptionHandler> logger) => _logger = logger;

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ColdPathUnavailableException)
        {
            return false;
        }

        _logger.LogWarning(exception, "Caminho frio indisponivel em {Path}.", httpContext.Request.Path);

        var problem = new ProblemDetails
        {
            Title = "Caminho frio indisponivel",
            Detail = "O ClickHouse nao respondeu. O painel ao vivo nao depende dele (RN-14).",
            Status = StatusCodes.Status503ServiceUnavailable,
            Instance = httpContext.Request.Path,
        };

        httpContext.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
        await httpContext.Response.WriteAsJsonAsync(problem, cancellationToken).ConfigureAwait(false);
        return true;
    }
}
