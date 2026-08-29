using System.Numerics;
using Alphractal.Fees.Api.Models.Domain;

namespace Alphractal.Fees.Api.Providers;

/// <summary>
/// Dados de bloco que o <c>newHeads</c> nao entrega. Interface propria para que
/// <c>Services/</c> nunca dependa da Nethereum (testavel sem rede).
/// </summary>
public interface IChainMetricsProvider
{
    /// <summary>
    /// Percentis de priority fee dos ultimos <paramref name="blockCount"/> blocos
    /// terminando em <paramref name="newestBlock"/> (RN-02).
    /// </summary>
    Task<IReadOnlyList<PriorityFeeSample>> GetPriorityFeeHistoryAsync(
        BigInteger newestBlock,
        int blockCount,
        CancellationToken cancellationToken);

    /// <summary>Numero de transacoes de um bloco — usado no spool (<c>tx_count</c>).</summary>
    Task<uint> GetTransactionCountAsync(BigInteger blockNumber, CancellationToken cancellationToken);
}
