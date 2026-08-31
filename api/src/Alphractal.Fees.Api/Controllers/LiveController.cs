using System.Text.Json;
using Alphractal.Fees.Api.Models.Domain;
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
    private readonly EthPriceBroadcaster _priceBroadcaster;
    private readonly HotBlockWindow _window;
    private readonly PriorityFeeState _tiers;
    private readonly FeeCalculator _calculator;

    public LiveController(
        FeesBroadcaster broadcaster,
        EthPriceBroadcaster priceBroadcaster,
        HotBlockWindow window,
        PriorityFeeState tiers,
        FeeCalculator calculator)
    {
        _broadcaster = broadcaster;
        _priceBroadcaster = priceBroadcaster;
        _window = window;
        _tiers = tiers;
        _calculator = calculator;
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

    /// <summary>Cotacao ETH/USD em tempo real, independente da cadencia dos blocos.</summary>
    [HttpGet("price-stream")]
    public async Task PriceStream(CancellationToken cancellationToken)
    {
        Response.ContentType = "text/event-stream; charset=utf-8";
        Response.Headers.CacheControl = "no-cache, no-store";
        Response.Headers.Connection = "keep-alive";
        Response.Headers["X-Accel-Buffering"] = "no";

        await Response.WriteAsync(": conectado\n\n", cancellationToken).ConfigureAwait(false);
        await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);

        try
        {
            await foreach (var price in _priceBroadcaster
                .SubscribeAsync(cancellationToken)
                .ConfigureAwait(false))
            {
                var response = new EthPriceTickResponse
                {
                    Price = price.Price,
                    ObservedAtUtc = price.ObservedAt,
                    Source = price.Source,
                };
                var payload = JsonSerializer.Serialize(response, SseJson);
                await Response.WriteAsync($"data: {payload}\n\n", cancellationToken).ConfigureAwait(false);
                await Response.Body.FlushAsync(cancellationToken).ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException)
        {
            // A aba foi fechada.
        }
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

    /// <summary>
    /// Custo de uma transacao com o gas limit informado, nas tres velocidades.
    /// </summary>
    /// <remarks>
    /// Servido da memoria (RN-14): usa a base fee do ultimo bloco e as faixas
    /// vigentes. Os gas limits da RN-11 sao referencias para os casos comuns;
    /// esta rota existe para quem conhece o gas exato da propria transacao —
    /// que e o caso do usuario institucional a que o projeto se destina.
    /// </remarks>
    [HttpGet("custo")]
    [Produces("application/json")]
    [ProducesResponseType<CustoPorGasResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult<CustoPorGasResponse> GetCusto([FromQuery] uint gasUnits)
    {
        // 21.000 e o minimo do protocolo para qualquer transacao; 30 milhoes e a
        // ordem do gas limit de um bloco inteiro. Fora disso o pedido nao
        // descreve uma transacao real.
        if (gasUnits is < 21_000 or > 30_000_000)
        {
            return Problem(
                title: "gasUnits fora de faixa",
                detail: "Informe entre 21.000 (transferencia simples) e 30.000.000 (bloco inteiro).",
                statusCode: StatusCodes.Status400BadRequest);
        }

        var bloco = _window.Latest;
        if (bloco is null)
        {
            return Problem(
                title: "Janela quente vazia",
                detail: "Nenhum bloco recebido ainda; nao ha base fee para calcular.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var faixas = _tiers.Current;
        var preco = _broadcaster.Latest?.EthUsd;

        var custos = new[] { SpeedTier.Slow, SpeedTier.Standard, SpeedTier.Fast }
            .Select(velocidade =>
            {
                var priority = faixas.For(velocidade);
                var wei = FeeCalculator.TransactionCostWei(bloco.BaseFeePerGas, priority, gasUnits);

                return new OperationCostResponse
                {
                    Operation = "personalizada",
                    Speed = velocidade switch
                    {
                        SpeedTier.Slow => "lento",
                        SpeedTier.Fast => "rapido",
                        _ => "padrao",
                    },
                    GasUnits = gasUnits,
                    TotalFeeGwei = FeeCalculator.ToGwei(wei),
                    TotalFeeEth = FeeCalculator.ToEth(wei),
                    TotalFeeUsd = preco is null ? null : FeeCalculator.ToUsd(wei, preco.Price),
                };
            })
            .ToList();

        return Ok(new CustoPorGasResponse
        {
            GasUnits = gasUnits,
            BlockNumber = (ulong)bloco.Number,
            BaseFeeGwei = FeeCalculator.ToGwei(bloco.BaseFeePerGas),
            Custos = custos,
            EthUsd = preco,
        });
    }

    /// <summary>
    /// Taxa de queima do EIP-1559, medida sobre a janela quente.
    /// </summary>
    /// <remarks>
    /// A base fee e destruida pelo protocolo, nao paga a ninguem. Medir isso
    /// converte congestionamento em impacto economico — e a leitura que um
    /// gestor entende sem saber o que e gwei.
    /// <para>
    /// A taxa por minuto usa o tempo real entre o primeiro e o ultimo bloco da
    /// janela, nao a suposicao de 12 s por bloco: slots perdidos existem, e
    /// assumir cadencia perfeita inflaria o numero justamente quando a rede
    /// esta com problema.
    /// </para>
    /// </remarks>
    [HttpGet("queima")]
    [Produces("application/json")]
    [ProducesResponseType<QueimaResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public ActionResult<QueimaResponse> GetQueima()
    {
        var blocos = _window.Snapshot(_window.Count);
        if (blocos.Count == 0)
        {
            return Problem(
                title: "Janela quente vazia",
                detail: "Nenhum bloco recebido ainda.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var queimadoWei = blocos.Aggregate(
            System.Numerics.BigInteger.Zero,
            static (total, bloco) => total + bloco.BaseFeePerGas * bloco.GasUsed);

        var maisNovo = blocos[0];
        var maisAntigo = blocos[^1];
        var minutos = (maisNovo.Timestamp - maisAntigo.Timestamp).TotalMinutes;

        var queimadoEth = FeeCalculator.ToEth(queimadoWei);
        // Com um unico bloco a janela tem duracao zero; nao da para extrapolar
        // uma taxa por minuto a partir de um ponto.
        var porMinuto = minutos > 0 ? queimadoEth / (decimal)minutos : 0m;
        var preco = _broadcaster.Latest?.EthUsd;

        return Ok(new QueimaResponse
        {
            EthPorMinuto = Math.Round(porMinuto, 6),
            EthNoUltimoBloco = FeeCalculator.ToEth(maisNovo.BaseFeePerGas * maisNovo.GasUsed),
            EthNaJanela = queimadoEth,
            BlocosNaJanela = blocos.Count,
            MinutosDaJanela = Math.Round(minutos, 2),
            UsdPorMinuto = preco is null ? null : Math.Round(porMinuto * preco.Price, 2),
        });
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
