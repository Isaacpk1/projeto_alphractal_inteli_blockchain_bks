using System.Text.Json;
using Alphractal.Fees.Api.Models.Responses;
using Alphractal.Fees.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Alphractal.Fees.Api.Controllers;

/// <summary>
/// Caminho quente: snapshot e stream, servidos da memoria (RN-14).
/// Nao toca no ClickHouse em nenhuma rota.
/// </summary>
/// <remarks>
/// O SSE vive numa action de controller retornando <see cref="IAsyncEnumerable{T}"/>
/// — e o pipeline MVC normal, sem view engine. E o argumento pratico da ADR-001:
/// Razor entrega HTML uma vez, no request; aqui o servidor empurra a cada bloco.
/// </remarks>
[ApiController]
[Route("api/v1/fees")]
public sealed class LiveController : ControllerBase
{
    /// <summary>
    /// camelCase explicito: o payload SSE nao passa pelo formatador do MVC, entao
    /// nao herda a convencao das outras rotas. Sem isto o front receberia
    /// PascalCase so neste endpoint.
    /// </summary>
    private static readonly JsonSerializerOptions SseJson = new(JsonSerializerDefaults.Web);

    private readonly FeesBroadcaster _broadcaster;
    private readonly HotBlockWindow _window;

    public LiveController(FeesBroadcaster broadcaster, HotBlockWindow window)
    {
        _broadcaster = broadcaster;
        _window = window;
    }

    /// <summary>Ultimo snapshot conhecido. Uma foto; o stream e o filme.</summary>
    [HttpGet("snapshot")]
    [Produces("application/json")]
    [ProducesResponseType<FeesSnapshotResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult<FeesSnapshotResponse> GetSnapshot()
    {
        var snapshot = _broadcaster.Latest;
        if (snapshot is null)
        {
            return Problem(
                title: "Janela quente vazia",
                detail: "Nenhum bloco recebido ainda. Verifique Fees:RpcWebSocketUrl e o log da ingestao. " +
                        "Na Ethereum o primeiro bloco chega em ate ~12 s apos a assinatura.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        return Ok(snapshot);
    }

    /// <summary>
    /// Stream SSE. O cliente recebe o ultimo snapshot na conexao (RN-13) e depois
    /// um evento por bloco.
    /// </summary>
    /// <remarks>
    /// O enquadramento e escrito a mao — <c>data: {json}\n\n</c> com flush por
    /// evento. Devolver <c>IAsyncEnumerable&lt;T&gt;</c> de uma action NAO produz
    /// SSE: o MVC serializa a sequencia como array JSON com
    /// <c>Content-Type: application/json</c>, e o <c>EventSource</c> conecta,
    /// recebe algo que nao sabe interpretar e nunca dispara <c>onmessage</c> —
    /// falha silenciosa, sem erro em lugar nenhum.
    /// <para>
    /// O <c>FlushAsync</c> por evento e obrigatorio: sem ele a resposta fica no
    /// buffer e os blocos chegam em lote, o que destroi o proposito do SSE e o
    /// orcamento de 2 s do RNF-01.
    /// </para>
    /// </remarks>
    [HttpGet("stream")]
    public async Task Stream(CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache, no-store";
        Response.Headers.Connection = "keep-alive";
        // Desliga o buffering de proxies reversos (nginx e afins).
        Response.Headers["X-Accel-Buffering"] = "no";

        // Comentario SSE inicial: fecha os headers e faz o EventSource disparar
        // onopen imediatamente, em vez de so no primeiro bloco (ate 12 s depois).
        await Response.WriteAsync(": conectado\n\n", cancellationToken).ConfigureAwait(false);
        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await foreach (var snapshot in _broadcaster
                .SubscribeAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                var payload = JsonSerializer.Serialize(snapshot, SseJson);

                await Response.WriteAsync($"id: {snapshot.BlockNumber}\n", cancellationToken).ConfigureAwait(false);
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken).ConfigureAwait(false);
                await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // Cliente fechou a aba. E o fim normal de um stream, nao um erro.
        }
    }

    /// <summary>Diagnostico da janela quente: quantos blocos ja entraram.</summary>
    [HttpGet("window")]
    [Produces("application/json")]
    public IActionResult GetWindow() => Ok(new
    {
        blocks = _window.Count,
        highestBlock = _window.HighestBlock.ToString(),
        subscribers = _broadcaster.SubscriberCount,
    });
}
