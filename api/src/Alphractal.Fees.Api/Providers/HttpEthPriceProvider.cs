using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using Alphractal.Fees.Api.Configuration;
using Alphractal.Fees.Api.Services;
using Microsoft.Extensions.Options;

namespace Alphractal.Fees.Api.Providers;

/// <summary>
/// Cotacao ETH/USD por HTTP, com cache do intervalo da RN-03 (15 s por padrao).
/// </summary>
/// <remarks>
/// Uma chamada por bloco (a cada ~12 s) estouraria o rate limit de qualquer fonte
/// gratuita em minutos, e a cotacao nao muda o suficiente nesse intervalo para
/// justificar. O cache e o que torna a fonte gratuita viavel.
/// <para>
/// Falha de rede devolve a ultima cotacao conhecida; se nunca houve nenhuma,
/// devolve <see cref="FeesOptions.FallbackEthUsd"/>, e se ele for zero devolve
/// <see cref="EthPrice.None"/>. Em nenhum caminho inventamos preco.
/// </para>
/// </remarks>
public sealed class HttpEthPriceProvider : BackgroundService, IEthPriceProvider
{
    public const string HttpClientName = "eth-price";
    private static readonly TimeSpan MinReconnectDelay = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxReconnectDelay = TimeSpan.FromSeconds(30);

    private readonly IHttpClientFactory _httpFactory;
    private readonly FeesOptions _options;
    private readonly EthPriceBroadcaster _broadcaster;
    private readonly ILogger<HttpEthPriceProvider> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _cacheGate = new();

    private EthPrice _cached = EthPrice.None;

    public HttpEthPriceProvider(
        IHttpClientFactory httpFactory,
        IOptions<FeesOptions> options,
        EthPriceBroadcaster broadcaster,
        ILogger<HttpEthPriceProvider> logger)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
        _broadcaster = broadcaster;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // Popula imediatamente pelo REST para que o painel nao espere o primeiro
        // negocio do feed. Depois o ticker passa a ser a fonte primaria.
        await GetAsync(stoppingToken).ConfigureAwait(false);

        if (string.IsNullOrWhiteSpace(_options.PriceWebSocketUrl))
        {
            _logger.LogInformation("WebSocket de cotacao desligado; usando polling REST.");
            return;
        }

        var reconnectDelay = MinReconnectDelay;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunWebSocketAsync(stoppingToken).ConfigureAwait(false);
                reconnectDelay = MinReconnectDelay;
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "WebSocket ETH/USD caiu; reconectando em {Delay}s. REST segue como fallback.",
                    reconnectDelay.TotalSeconds);
            }

            try
            {
                await Task.Delay(reconnectDelay, stoppingToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }

            reconnectDelay = TimeSpan.FromTicks(
                Math.Min(reconnectDelay.Ticks * 2, MaxReconnectDelay.Ticks));
        }
    }

    public async Task<EthPrice> GetAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var cached = ReadCached();

        if (cached.HasValue && (now - cached.ObservedAt).TotalSeconds < _options.PriceRefreshSeconds)
        {
            return cached;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Outra requisicao pode ter atualizado enquanto esperavamos o lock.
            now = DateTimeOffset.UtcNow;
            cached = ReadCached();
            if (cached.HasValue && (now - cached.ObservedAt).TotalSeconds < _options.PriceRefreshSeconds)
            {
                return cached;
            }

            if (string.IsNullOrWhiteSpace(_options.PriceSourceUrl))
            {
                return Fallback(now);
            }

            var fetched = await FetchAsync(cancellationToken).ConfigureAwait(false);
            if (fetched > 0)
            {
                return Store(fetched, now, SourceLabel());
            }

            cached = ReadCached();
            return cached.HasValue ? cached : Fallback(now);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Falha ao obter cotacao ETH/USD.");
            cached = ReadCached();
            return cached.HasValue ? cached : Fallback(DateTimeOffset.UtcNow);
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task RunWebSocketAsync(CancellationToken cancellationToken)
    {
        using var socket = new ClientWebSocket();
        await socket
            .ConnectAsync(new Uri(_options.PriceWebSocketUrl), cancellationToken)
            .ConfigureAwait(false);

        await SendSubscriptionAsync(socket, "ticker", cancellationToken).ConfigureAwait(false);
        await SendSubscriptionAsync(socket, "heartbeats", cancellationToken).ConfigureAwait(false);
        _logger.LogInformation("Cotacao ETH/USD ao vivo assinada no ticker da Coinbase.");

        var buffer = new byte[16 * 1024];
        while (socket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
        {
            using var payload = new MemoryStream();
            WebSocketReceiveResult result;
            do
            {
                result = await socket.ReceiveAsync(buffer, cancellationToken).ConfigureAwait(false);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    throw new WebSocketException($"Feed fechou a conexao: {result.CloseStatus}.");
                }

                payload.Write(buffer, 0, result.Count);
            }
            while (!result.EndOfMessage);

            if (result.MessageType != WebSocketMessageType.Text)
            {
                continue;
            }

            payload.Position = 0;
            using var document = await JsonDocument
                .ParseAsync(payload, cancellationToken: cancellationToken)
                .ConfigureAwait(false);
            var price = ExtractTickerPrice(document.RootElement);
            if (price > 0)
            {
                Store(price, DateTimeOffset.UtcNow, "advanced-trade-ws.coinbase.com");
            }
        }
    }

    private static async Task SendSubscriptionAsync(
        ClientWebSocket socket,
        string channel,
        CancellationToken cancellationToken)
    {
        var message = channel == "ticker"
            ? """{"type":"subscribe","product_ids":["ETH-USD"],"channel":"ticker"}"""
            : """{"type":"subscribe","channel":"heartbeats"}""";
        var bytes = Encoding.UTF8.GetBytes(message);
        await socket
            .SendAsync(bytes, WebSocketMessageType.Text, true, cancellationToken)
            .ConfigureAwait(false);
    }

    internal static decimal ExtractTickerPrice(JsonElement root)
    {
        if (!root.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
        {
            return 0m;
        }

        foreach (var marketEvent in events.EnumerateArray())
        {
            if (!marketEvent.TryGetProperty("tickers", out var tickers)
                || tickers.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var ticker in tickers.EnumerateArray())
            {
                if (!ticker.TryGetProperty("product_id", out var product)
                    || product.GetString() != "ETH-USD"
                    || !ticker.TryGetProperty("price", out var price))
                {
                    continue;
                }

                if (price.ValueKind == JsonValueKind.String
                    && decimal.TryParse(
                        price.GetString(),
                        System.Globalization.NumberStyles.Any,
                        System.Globalization.CultureInfo.InvariantCulture,
                        out var parsed))
                {
                    return parsed;
                }
            }
        }

        return 0m;
    }

    private EthPrice ReadCached()
    {
        lock (_cacheGate)
        {
            return _cached;
        }
    }

    private EthPrice Store(decimal price, DateTimeOffset observedAt, string source)
    {
        var next = new EthPrice(price, observedAt, source);
        bool changed;
        lock (_cacheGate)
        {
            changed = !_cached.HasValue || _cached.Price != price;
            _cached = next;
        }

        if (changed)
        {
            _broadcaster.Publish(next);
        }

        return next;
    }

    /// <summary>
    /// Procedencia da cotacao, derivada da URL configurada.
    /// </summary>
    /// <remarks>
    /// Derivada, e nao constante: este valor vai para a coluna <c>source</c> de
    /// <c>eth_usd_prices</c>. Um rotulo chumbado registra procedencia errada no
    /// banco quando alguem troca a fonte por configuracao — e procedencia errada
    /// em metrica financeira invalida a auditoria, sem dar nenhum sinal.
    /// </remarks>
    private string SourceLabel()
        => Uri.TryCreate(_options.PriceSourceUrl, UriKind.Absolute, out var uri)
            ? uri.Host
            : "desconhecida";

    private EthPrice Fallback(DateTimeOffset now)
        => _options.FallbackEthUsd > 0
            ? new EthPrice(_options.FallbackEthUsd, now, "fallback")
            : EthPrice.None;

    private async Task<decimal> FetchAsync(CancellationToken cancellationToken)
    {
        var http = _httpFactory.CreateClient(HttpClientName);

        using var response = await http
            .GetAsync(_options.PriceSourceUrl, cancellationToken)
            .ConfigureAwait(false);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("Fonte de cotacao respondeu {Status}.", (int)response.StatusCode);
            return 0m;
        }

        await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var document = await JsonDocument.ParseAsync(stream, cancellationToken: cancellationToken).ConfigureAwait(false);

        return ExtractPrice(document.RootElement, _options.PriceJsonPath, _logger);
    }

    /// <summary>
    /// Le o valor no caminho configurado (ex.: <c>data.amount</c> na Coinbase,
    /// <c>ethereum.usd</c> no CoinGecko).
    /// </summary>
    /// <remarks>
    /// <c>internal</c> e estatico para ser testavel sem rede: o formato da
    /// resposta e a parte que muda quando o provedor muda, e e onde o erro
    /// aparece calado — devolver 0 significa "sem cotacao" e o painel some com o
    /// USD sem dizer por que.
    /// </remarks>
    internal static decimal ExtractPrice(JsonElement root, string path, ILogger logger)
    {
        var current = root;

        foreach (var segment in path.Split('.', StringSplitOptions.RemoveEmptyEntries))
        {
            if (current.ValueKind != JsonValueKind.Object
                || !current.TryGetProperty(segment, out current))
            {
                logger.LogWarning("Cotacao: caminho '{Path}' nao existe na resposta.", path);
                return 0m;
            }
        }

        // Coinbase devolve o valor como string ("3200.00"); CoinGecko, como numero.
        return current.ValueKind switch
        {
            JsonValueKind.Number when current.TryGetDecimal(out var number) => number,
            JsonValueKind.String when decimal.TryParse(
                current.GetString(),
                System.Globalization.NumberStyles.Any,
                System.Globalization.CultureInfo.InvariantCulture,
                out var parsed) => parsed,
            _ => 0m,
        };
    }
}
