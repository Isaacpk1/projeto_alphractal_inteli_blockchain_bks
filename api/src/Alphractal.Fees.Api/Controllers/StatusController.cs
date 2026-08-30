using Alphractal.Fees.Api.Models.Responses;
using Alphractal.Fees.Api.Repositories;
using Microsoft.AspNetCore.Mvc;

namespace Alphractal.Fees.Api.Controllers;

/// <summary>
/// Saude da ingestao, lida de <c>v_ingestion_status</c>. Complementa o
/// <see cref="HealthController"/>, que so responde se o processo esta de pe.
/// </summary>
[ApiController]
[Route("api/v1/status")]
[Produces("application/json")]
public sealed class StatusController : ControllerBase
{
    private readonly IFeesHistoryRepository _repository;

    public StatusController(IFeesHistoryRepository repository) => _repository = repository;

    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var coldPathUp = await _repository.PingAsync(cancellationToken);
        if (!coldPathUp)
        {
            // Caminho frio fora do ar nao e erro da API: o painel ao vivo segue
            // funcionando sem ele. Respondemos 200 com o estado explicito.
            return Ok(new
            {
                coldPath = "down",
                components = Array.Empty<ComponentStatusResponse>(),
            });
        }

        var now = DateTimeOffset.UtcNow;
        var components = await _repository.GetIngestionStatusAsync(cancellationToken);

        return Ok(new
        {
            coldPath = "up",
            components = components.Select(item => new ComponentStatusResponse
            {
                Component = item.Component,
                Status = item.Status,
                LagMs = item.LagMs,
                LastBlock = item.LastBlock,
                Detail = item.Detail,
                LastSeenAtUtc = item.LastSeenAt,
                SecondsSinceLastSeen = Math.Max(0, (now - item.LastSeenAt).TotalSeconds),
            }).ToList(),
        });
    }
}
