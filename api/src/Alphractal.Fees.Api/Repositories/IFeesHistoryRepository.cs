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

    /// <summary>
    /// Distribuicao da base fee nos ultimos 30 dias (D-02). <c>null</c> quando
    /// nao ha nenhum bucket na janela.
    /// </summary>
    Task<ColdBaseFeeDistribution?> GetBaseFeeDistributionAsync(CancellationToken cancellationToken);

    /// <summary>Media da base fee por hora do dia (UTC), 30 dias.</summary>
    Task<IReadOnlyList<ColdHoraDoDia>> GetHoraDoDiaAsync(CancellationToken cancellationToken);

    /// <summary>Grade dia-da-semana x hora para o heatmap.</summary>
    Task<IReadOnlyList<ColdSemanaHora>> GetSemanaHoraAsync(CancellationToken cancellationToken);

    /// <summary>Cotacao atual e de 24 h atras. <c>null</c> se nao ha serie.</summary>
    Task<ColdEthUsd24h?> GetEthUsd24hAsync(CancellationToken cancellationToken);

    /// <summary>Ping de diagnostico. <c>false</c> se o banco nao respondeu.</summary>
    Task<bool> PingAsync(CancellationToken cancellationToken);
}
