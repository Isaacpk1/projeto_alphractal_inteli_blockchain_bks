using Alphractal.Fees.Api.Models.Domain;
using Alphractal.Fees.Api.Models.Domain.ColdPath;

namespace Alphractal.Fees.Api.Repositories;

/// <summary>
/// Leitura do caminho frio. Nao calcula nada e nao converte unidade: as views
/// <c>v_*</c> ja entregam gwei, ETH e USD.
/// </summary>
/// <remarks>
/// O caminho quente NAO passa por aqui (RN-14). Servir o SSE a partir daqui
/// entregaria dado com frescor de ~1 minuto e tornaria o RNF-01 impossivel.
/// </remarks>
public interface IFeesHistoryRepository
{
    Task<ColdLatestBlock?> GetLatestBlockAsync(CancellationToken cancellationToken);

    Task<ColdMempoolSample?> GetMempoolNowAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ColdFeeEstimate>> GetFeeEstimatesNowAsync(CancellationToken cancellationToken);

    Task<IReadOnlyList<ColdFeeHistoryPoint>> GetFeeHistoryAsync(
        HistoryGranularity granularity,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ColdFeeEstimateDaily>> GetFeeEstimatesDailyAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int limit,
        CancellationToken cancellationToken);

    Task<IReadOnlyList<ColdComponentHealth>> GetIngestionStatusAsync(CancellationToken cancellationToken);

    /// <summary>Ping de diagnostico. <c>false</c> se o banco nao respondeu.</summary>
    Task<bool> PingAsync(CancellationToken cancellationToken);
}
