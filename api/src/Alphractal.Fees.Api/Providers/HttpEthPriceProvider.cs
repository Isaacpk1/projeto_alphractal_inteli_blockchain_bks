using System.Text.Json;
using Alphractal.Fees.Api.Configuration;
using Microsoft.Extensions.Options;

namespace Alphractal.Fees.Api.Providers;

/// <summary>
/// Cotacao ETH/USD por HTTP, com cache do intervalo da RN-03 (60 s por padrao).
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
public sealed class HttpEthPriceProvider : IEthPriceProvider
{
    public const string HttpClientName = "eth-price";

    private readonly IHttpClientFactory _httpFactory;
    private readonly FeesOptions _options;
    private readonly ILogger<HttpEthPriceProvider> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private EthPrice _cached = EthPrice.None;

    public HttpEthPriceProvider(
        IHttpClientFactory httpFactory,
        IOptions<FeesOptions> options,
        ILogger<HttpEthPriceProvider> logger)
    {
        _httpFactory = httpFactory;
        _options = options.Value;
        _logger = logger;
    }

    public async Task<EthPrice> GetAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        if (_cached.HasValue && (now - _cached.ObservedAt).TotalSeconds < _options.PriceRefreshSeconds)
        {
            return _cached;
        }

        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Outra requisicao pode ter atualizado enquanto esperavamos o lock.
            now = DateTimeOffset.UtcNow;
            if (_cached.HasValue && (now - _cached.ObservedAt).TotalSeconds < _options.PriceRefreshSeconds)
            {
                return _cached;
            }

            if (string.IsNullOrWhiteSpace(_options.PriceSourceUrl))
            {
                return Fallback(now);
            }

            var fetched = await FetchAsync(cancellationToken).ConfigureAwait(false);
            if (fetched > 0)
            {
                _cached = new EthPrice(fetched, now, "coingecko");
                return _cached;
            }

            return _cached.HasValue ? _cached : Fallback(now);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Falha ao obter cotacao ETH/USD.");
            return _cached.HasValue ? _cached : Fallback(DateTimeOffset.UtcNow);
        }
        finally
        {
            _gate.Release();
        }
    }

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
