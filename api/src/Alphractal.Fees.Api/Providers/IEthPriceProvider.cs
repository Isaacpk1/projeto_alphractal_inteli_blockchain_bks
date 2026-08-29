namespace Alphractal.Fees.Api.Providers;

/// <summary>Cotacao ETH/USD para a conversao da RN-03.</summary>
public interface IEthPriceProvider
{
    /// <summary>
    /// Ultima cotacao conhecida. <c>Price</c> zero significa "sem cotacao" — o
    /// consumidor deve omitir o valor em USD, nunca exibir zero como se fosse preco.
    /// </summary>
    Task<EthPrice> GetAsync(CancellationToken cancellationToken);
}

/// <param name="Price">USD por ETH. Zero = indisponivel.</param>
/// <param name="ObservedAt">Quando esta cotacao foi obtida.</param>
/// <param name="Source">Origem, para auditoria e para o campo <c>source</c> do spool.</param>
public readonly record struct EthPrice(decimal Price, DateTimeOffset ObservedAt, string Source)
{
    public bool HasValue => Price > 0;

    public static EthPrice None => new(0m, DateTimeOffset.MinValue, "none");
}
