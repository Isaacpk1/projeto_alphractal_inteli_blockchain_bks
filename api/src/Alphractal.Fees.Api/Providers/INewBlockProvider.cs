using Alphractal.Fees.Api.Models.Domain;

namespace Alphractal.Fees.Api.Providers;

/// <summary>
/// Fonte de novos blocos. O <c>Services/</c> depende desta interface, nunca da
/// Nethereum — e o que permite testar as regras sem rede.
/// </summary>
public interface INewBlockProvider
{
    /// <summary>
    /// Consome blocos ate o cancelamento. Reconexao e backoff sao
    /// responsabilidade da implementacao, nao de quem chama.
    /// </summary>
    Task RunAsync(Func<ChainBlockHeader, CancellationToken, Task> onBlock, CancellationToken cancellationToken);
}
