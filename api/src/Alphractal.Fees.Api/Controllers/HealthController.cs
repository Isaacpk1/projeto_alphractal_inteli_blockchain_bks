using Microsoft.AspNetCore.Mvc;

namespace Alphractal.Fees.Api.Controllers;

/// <summary>
/// Liveness da API. Nao reflete a saude da ingestao — isso e responsabilidade
/// do endpoint de status, alimentado por ingestion_health.
/// </summary>
[ApiController]
[Route("api/v1/health")]
public sealed class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get() => Ok(new
    {
        status = "ok",
        utc = DateTimeOffset.UtcNow,
    });
}
