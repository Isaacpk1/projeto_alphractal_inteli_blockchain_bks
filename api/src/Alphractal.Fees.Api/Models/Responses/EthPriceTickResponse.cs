namespace Alphractal.Fees.Api.Models.Responses;

/// <summary>Cotacao ETH/USD recebida em tempo real da fonte de mercado.</summary>
public sealed record EthPriceTickResponse
{
    public required decimal Price { get; init; }
    public required DateTimeOffset ObservedAtUtc { get; init; }
    public required string Source { get; init; }
}
