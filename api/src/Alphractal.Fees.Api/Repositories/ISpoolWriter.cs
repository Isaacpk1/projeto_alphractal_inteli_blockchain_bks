using Alphractal.Fees.Api.Models.Domain;
using Alphractal.Fees.Api.Providers;

namespace Alphractal.Fees.Api.Repositories;

/// <summary>Escrita append-only do NDJSON que o ETL consome.</summary>
public interface ISpoolWriter : IAsyncDisposable
{
    /// <summary>Enfileira um bloco e suas estimativas no lote corrente.</summary>
    Task WriteBlockAsync(
        ChainBlockHeader block,
        System.Numerics.BigInteger nextBaseFee,
        PriorityFeeSample tiers,
        uint transactionCount,
        IReadOnlyList<FeeEstimate> estimates,
        EthPrice price,
        CancellationToken cancellationToken);

    /// <summary>
    /// Registra uma amostra de mempool (<c>mempool_samples</c>).
    /// </summary>
    /// <remarks>
    /// Amostragem sub-bloco: acontece entre blocos, num intervalo proprio, porque
    /// o valor dela e justamente mostrar movimento quando nada mais se move.
    /// </remarks>
    Task WriteMempoolSampleAsync(
        DateTimeOffset sampledAt,
        System.Numerics.BigInteger blockNumber,
        uint pendingTxCount,
        System.Numerics.BigInteger baseFeePerGas,
        PriorityFeeSample tiers,
        EthPrice price,
        CancellationToken cancellationToken);

    /// <summary>
    /// Registra o heartbeat de um componente (RF de saude da ingestao).
    /// </summary>
    /// <remarks>
    /// Metodo separado de <see cref="WriteBlockAsync"/> de proposito: o heartbeat
    /// nao depende de cotacao, e justamente quando o preco falha e que saber que
    /// a ingestao esta viva importa mais.
    /// <para>
    /// Vai pelo spool porque a API NAO tem permissao de INSERT no ClickHouse —
    /// <c>alphractal_api</c> so tem SELECT nas views (005_users.sql). O ETL e o
    /// unico escritor do banco, e essa separacao nao se abre por conveniencia.
    /// </para>
    /// </remarks>
    Task WriteHealthAsync(
        string component,
        string status,
        long lagMs,
        System.Numerics.BigInteger lastBlock,
        string detail,
        CancellationToken cancellationToken);

    /// <summary>Fecha o lote corrente e o move para <c>ready/</c>, se houver conteudo.</summary>
    Task FlushAsync(CancellationToken cancellationToken);
}
