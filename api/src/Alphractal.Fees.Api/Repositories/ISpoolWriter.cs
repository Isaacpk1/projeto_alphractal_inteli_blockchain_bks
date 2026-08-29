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

    /// <summary>Fecha o lote corrente e o move para <c>ready/</c>, se houver conteudo.</summary>
    Task FlushAsync(CancellationToken cancellationToken);
}
