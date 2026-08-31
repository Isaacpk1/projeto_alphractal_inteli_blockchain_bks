using System.Text.Json;
using Alphractal.Fees.Api.Providers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Alphractal.Fees.Tests;

/// <summary>
/// Leitura da cotacao no formato de cada provedor. Testado porque a falha aqui e
/// muda: devolver 0 significa "sem cotacao", o USD some do painel e o spool para
/// de escrever — sem nenhum erro visivel.
/// </summary>
public sealed class EthPriceParsingTests
{
    private static decimal Extract(string json, string path)
        => HttpEthPriceProvider.ExtractPrice(
            JsonDocument.Parse(json).RootElement, path, NullLogger.Instance);

    [Fact]
    public void Coinbase_devolve_o_valor_como_string()
    {
        var json = """{"data":{"amount":"3200.55","base":"ETH","currency":"USD"}}""";

        Assert.Equal(3200.55m, Extract(json, "data.amount"));
    }

    [Fact]
    public void Coingecko_devolve_o_valor_como_numero()
    {
        var json = """{"ethereum":{"usd":3200.55}}""";

        Assert.Equal(3200.55m, Extract(json, "ethereum.usd"));
    }

    [Fact]
    public void Caminho_inexistente_devolve_zero_e_nao_lanca()
    {
        // 403 com corpo de erro, mudanca de formato do provedor: nenhum dos dois
        // pode derrubar a ingestao.
        var json = """{"errors":[{"id":"not_found"}]}""";

        Assert.Equal(0m, Extract(json, "data.amount"));
    }

    [Fact]
    public void Valor_nao_numerico_devolve_zero()
    {
        var json = """{"data":{"amount":"indisponivel"}}""";

        Assert.Equal(0m, Extract(json, "data.amount"));
    }

    [Fact]
    public void Ponto_decimal_e_lido_como_invariante()
    {
        // A maquina esta em pt-BR, onde a virgula e o separador decimal. Sem
        // InvariantCulture, "3200.55" viraria 320055.
        var json = """{"data":{"amount":"3200.55"}}""";

        Assert.Equal(3200.55m, Extract(json, "data.amount"));
    }

    [Fact]
    public void Coinbase_ticker_extrai_eth_usd_e_ignora_outros_produtos()
    {
        var json = """
            {
              "channel":"ticker",
              "events":[{"tickers":[
                {"product_id":"BTC-USD","price":"65000.00"},
                {"product_id":"ETH-USD","price":"2423.04"}
              ]}]
            }
            """;

        using var document = JsonDocument.Parse(json);
        Assert.Equal(2423.04m, HttpEthPriceProvider.ExtractTickerPrice(document.RootElement));
    }

    [Fact]
    public void Coinbase_heartbeat_nao_e_interpretado_como_preco()
    {
        var json = """{"channel":"heartbeats","events":[{"heartbeat_counter":"10"}]}""";

        using var document = JsonDocument.Parse(json);
        Assert.Equal(0m, HttpEthPriceProvider.ExtractTickerPrice(document.RootElement));
    }
}
