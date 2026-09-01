using System.Data;
using System.Data.Common;
using Alphractal.Fees.Api.Models.Domain;
using Alphractal.Fees.Api.Models.Domain.ColdPath;
using Alphractal.Fees.Api.Repositories.Sql;

namespace Alphractal.Fees.Api.Repositories;

/// <summary>
/// Implementacao do caminho frio sobre as views <c>v_*</c> do ClickHouse.
/// </summary>
public sealed class ClickHouseFeesHistoryRepository : IFeesHistoryRepository
{
    private readonly IClickHouseConnectionFactory _factory;
    private readonly ILogger<ClickHouseFeesHistoryRepository> _logger;

    public ClickHouseFeesHistoryRepository(
        IClickHouseConnectionFactory factory,
        ILogger<ClickHouseFeesHistoryRepository> logger)
    {
        _factory = factory;
        _logger = logger;
    }

    public Task<ColdLatestBlock?> GetLatestBlockAsync(CancellationToken cancellationToken)
        => QuerySingleAsync(SqlResources.LatestBlock, null, static reader => new ColdLatestBlock
        {
            BlockNumber = reader.AsUInt64("block_number"),
            BlockTimestamp = reader.AsUtc("block_timestamp"),
            BaseFeeGwei = reader.AsDouble("base_fee_gwei"),
            NextBaseFeeGwei = reader.AsDouble("next_base_fee_gwei"),
            PriorityFeeGwei = reader.AsDouble("priority_fee_gwei"),
            GasUsed = reader.AsUInt64("gas_used"),
            GasLimit = reader.AsUInt64("gas_limit"),
            GasUsedRatio = reader.AsDouble("gas_used_ratio"),
            TxCount = reader.AsUInt32("tx_count"),
            BurnedEth = reader.AsDouble("burned_eth"),
            TotalFeeEth = reader.AsDouble("total_fee_eth"),
            EthUsd = reader.AsDecimal("eth_usd"),
            AgeMs = reader.AsInt64("age_ms"),
        }, cancellationToken);

    public Task<ColdMempoolSample?> GetMempoolNowAsync(CancellationToken cancellationToken)
        => QuerySingleAsync(SqlResources.MempoolNow, null, static reader => new ColdMempoolSample
        {
            SampledAt = reader.AsUtc("sampled_at"),
            BlockNumber = reader.AsUInt64("block_number"),
            PendingBlockTxCount = reader.AsUInt32("pending_tx_count"),
            BaseFeeGwei = reader.AsDouble("base_fee_gwei"),
            PrioritySlowGwei = reader.AsDouble("priority_slow_gwei"),
            PriorityStandardGwei = reader.AsDouble("priority_standard_gwei"),
            PriorityFastGwei = reader.AsDouble("priority_fast_gwei"),
            EthUsd = reader.AsDecimal("eth_usd"),
        }, cancellationToken);

    public Task<IReadOnlyList<ColdFeeEstimate>> GetFeeEstimatesNowAsync(CancellationToken cancellationToken)
        => QueryAsync(SqlResources.FeeEstimatesNow, null, static reader => new ColdFeeEstimate
        {
            Operation = reader.AsString("operation"),
            Speed = reader.AsString("speed"),
            GasUnits = reader.AsUInt32("gas_units"),
            TotalFeeGwei = reader.AsDecimal("total_fee_gwei"),
            TotalFeeUsd = reader.AsDecimal("total_fee_usd"),
            LastSampledAt = reader.AsUtc("last_sampled_at"),
        }, cancellationToken);

    public Task<IReadOnlyList<ColdFeeHistoryPoint>> GetFeeHistoryAsync(
        HistoryGranularity granularity,
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int limit,
        CancellationToken cancellationToken)
    {
        // A granularidade escolhe a consulta, nunca compoe SQL: e enum, entao nao
        // existe caminho de injecao pelo nome da view.
        var sql = granularity == HistoryGranularity.Day
            ? SqlResources.FeesHistoryDaily
            : SqlResources.FeesHistoryHourly;

        return QueryAsync(sql, command => AddRangeParameters(command, fromUtc, toUtc, limit),
            static reader => new ColdFeeHistoryPoint
            {
                Bucket = reader.AsUtc("bucket"),
                Blocks = reader.AsUInt64("blocks"),
                BaseFeeGweiAvg = reader.AsDouble("base_fee_gwei_avg"),
                BaseFeeGweiMin = reader.AsDouble("base_fee_gwei_min"),
                BaseFeeGweiMax = reader.AsDouble("base_fee_gwei_max"),
                BaseFeeGweiP50 = reader.AsDouble("base_fee_gwei_p50"),
                BaseFeeGweiP90 = reader.AsDouble("base_fee_gwei_p90"),
                BaseFeeGweiP95 = reader.AsDouble("base_fee_gwei_p95"),
                PriorityFeeGweiAvg = reader.AsDouble("priority_fee_gwei_avg"),
                GasUsedRatioAvg = reader.AsDouble("gas_used_ratio_avg"),
                TxCount = reader.AsUInt64("tx_count"),
                BurnedEth = reader.AsDouble("burned_eth"),
                TotalFeeEth = reader.AsDouble("total_fee_eth"),
                EthUsdAvg = reader.AsDecimal("eth_usd_avg"),
            }, cancellationToken);
    }

    public Task<IReadOnlyList<ColdFeeEstimateDaily>> GetFeeEstimatesDailyAsync(
        DateTimeOffset fromUtc,
        DateTimeOffset toUtc,
        int limit,
        CancellationToken cancellationToken)
        => QueryAsync(SqlResources.FeeEstimatesDaily,
            command => AddRangeParameters(command, fromUtc, toUtc, limit),
            static reader => new ColdFeeEstimateDaily
            {
                Bucket = reader.AsDate("bucket"),
                Operation = reader.AsString("operation"),
                Speed = reader.AsString("speed"),
                Samples = reader.AsUInt64("samples"),
                UsdAvg = reader.AsDecimal("usd_avg"),
                UsdMin = reader.AsDecimal("usd_min"),
                UsdMax = reader.AsDecimal("usd_max"),
                UsdP50 = reader.AsDecimal("usd_p50"),
                UsdP90 = reader.AsDecimal("usd_p90"),
            }, cancellationToken);

    public Task<IReadOnlyList<ColdComponentHealth>> GetIngestionStatusAsync(CancellationToken cancellationToken)
        => QueryAsync(SqlResources.IngestionStatus, null, static reader => new ColdComponentHealth
        {
            Component = reader.AsString("component"),
            Status = reader.AsString("status"),
            LagMs = reader.AsUInt32("lag_ms"),
            LastBlock = reader.AsUInt64("last_block"),
            Detail = reader.AsString("detail"),
            LastSeenAt = reader.AsUtc("last_seen_at"),
        }, cancellationToken);

    public async Task<ColdBaseFeeDistribution?> GetBaseFeeDistributionAsync(CancellationToken cancellationToken)
    {
        var distribution = await QuerySingleAsync(
            SqlResources.BaseFeePercentiles30d, null, static reader => new ColdBaseFeeDistribution
            {
                Buckets = reader.AsUInt64("buckets"),
                FromBucket = reader.AsUtc("from_bucket"),
                ToBucket = reader.AsUtc("to_bucket"),
                P05Gwei = reader.AsDouble("p05_gwei"),
                P10Gwei = reader.AsDouble("p10_gwei"),
                P25Gwei = reader.AsDouble("p25_gwei"),
                P50Gwei = reader.AsDouble("p50_gwei"),
                P75Gwei = reader.AsDouble("p75_gwei"),
                P90Gwei = reader.AsDouble("p90_gwei"),
                P95Gwei = reader.AsDouble("p95_gwei"),
                MinGwei = reader.AsDouble("min_gwei"),
                MaxGwei = reader.AsDouble("max_gwei"),
            }, cancellationToken).ConfigureAwait(false);

        // A view sempre devolve UMA linha, mesmo sem dado — agregacao sem GROUP BY
        // nao retorna vazio. Zero buckets significa janela sem dado, e devolver
        // null aqui evita que o consumidor compare contra percentis todos zerados.
        return distribution is { Buckets: 0 } ? null : distribution;
    }

    public Task<IReadOnlyList<ColdHoraDoDia>> GetHoraDoDiaAsync(CancellationToken cancellationToken)
        => QueryAsync(SqlResources.FeesHoraDoDia, null, static reader => new ColdHoraDoDia
        {
            HoraUtc = (int)reader.AsUInt32("hora_utc"),
            Amostras = reader.AsUInt64("amostras"),
            BaseFeeGweiAvg = reader.AsDouble("base_fee_gwei_avg"),
            BaseFeeGweiP50 = reader.AsDouble("base_fee_gwei_p50"),
            BaseFeeGweiMin = reader.AsDouble("base_fee_gwei_min"),
            BaseFeeGweiMax = reader.AsDouble("base_fee_gwei_max"),
        }, cancellationToken);

    public Task<IReadOnlyList<ColdSemanaHora>> GetSemanaHoraAsync(CancellationToken cancellationToken)
        => QueryAsync(SqlResources.FeesSemanaHora, null, static reader => new ColdSemanaHora
        {
            DiaSemana = (int)reader.AsUInt32("dia_semana"),
            HoraUtc = (int)reader.AsUInt32("hora_utc"),
            Amostras = reader.AsUInt64("amostras"),
            BaseFeeGweiAvg = reader.AsDouble("base_fee_gwei_avg"),
        }, cancellationToken);

    public async Task<ColdEthUsd24h?> GetEthUsd24hAsync(CancellationToken cancellationToken)
    {
        var linha = await QuerySingleAsync(SqlResources.EthUsd24h, null, static reader => new ColdEthUsd24h
        {
            PrecoAtual = reader.AsDecimal("preco_atual"),
            ObservadoEm = reader.AsUtc("observado_em"),
            Preco24h = reader.AsDecimal("preco_24h"),
            ObservadoEm24h = reader.AsUtc("observado_em_24h"),
            Amostras24h = reader.AsUInt64("amostras_24h"),
        }, cancellationToken).ConfigureAwait(false);

        // Agregacao sem GROUP BY devolve sempre uma linha, zerada quando nao ha
        // dado. Preco atual zero significa serie vazia, nao ETH valendo zero.
        return linha is null || linha.PrecoAtual <= 0 ? null : linha;
    }

    public async Task<bool> PingAsync(CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT 1";
            command.CommandTimeout = _factory.CommandTimeoutSeconds;
            var result = await command.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
            return result is not null;
        }
        catch (ColdPathUnavailableException)
        {
            return false;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(exception, "Ping do ClickHouse falhou.");
            return false;
        }
    }

    private static void AddRangeParameters(DbCommand command, DateTimeOffset fromUtc, DateTimeOffset toUtc, int limit)
    {
        AddParameter(command, "from", fromUtc.UtcDateTime);
        AddParameter(command, "to", toUtc.UtcDateTime);
        AddParameter(command, "limit", limit);
    }

    private static void AddParameter(DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }

    private async Task<T?> QuerySingleAsync<T>(
        string sql,
        Action<DbCommand>? bind,
        Func<DbDataReader, T> map,
        CancellationToken cancellationToken)
        where T : class
    {
        var rows = await QueryAsync(sql, bind, map, cancellationToken).ConfigureAwait(false);
        return rows.Count == 0 ? null : rows[0];
    }

    private async Task<IReadOnlyList<T>> QueryAsync<T>(
        string sql,
        Action<DbCommand>? bind,
        Func<DbDataReader, T> map,
        CancellationToken cancellationToken)
    {
        try
        {
            await using var connection = await _factory.OpenAsync(cancellationToken).ConfigureAwait(false);
            await using var command = connection.CreateCommand();
            command.CommandText = sql;
            command.CommandType = CommandType.Text;
            command.CommandTimeout = _factory.CommandTimeoutSeconds;
            bind?.Invoke(command);

            var rows = new List<T>();
            await using var reader = await command
                .ExecuteReaderAsync(cancellationToken)
                .ConfigureAwait(false);

            while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
            {
                rows.Add(map(reader));
            }

            return rows;
        }
        catch (ColdPathUnavailableException)
        {
            throw;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Consulta ao ClickHouse falhou.");
            throw new ColdPathUnavailableException("Consulta ao caminho frio falhou.", exception);
        }
    }
}
