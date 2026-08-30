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

    /// <summary>
    /// Transacoes aguardando inclusao no momento da chamada (<c>pending</c>).
    /// </summary>
    /// <remarks>
    /// Amostra sub-bloco: e o unico sinal do sistema que se move ENTRE blocos.
    /// Todo o resto so muda a cada ~12 s, entao o mempool e o que mostra pressao
    /// se acumulando antes de virar base fee.
    /// <para>
    /// Numero aproximado por natureza: cada no vê um mempool diferente, e o que a
    /// Alchemy reporta e a visao dela. Serve para tendencia, nao para contagem
    /// exata — e o painel deve apresentar assim.
    /// </para>
    /// </remarks>
    Task<uint> GetPendingTransactionCountAsync(CancellationToken cancellationToken);
}
