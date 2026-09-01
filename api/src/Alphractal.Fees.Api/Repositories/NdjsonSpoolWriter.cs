using System.Globalization;
using System.Numerics;
using System.Text;
using System.Text.Json;
using Alphractal.Fees.Api.Configuration;
using Alphractal.Fees.Api.Models.Domain;
using Alphractal.Fees.Api.Providers;
using Microsoft.Extensions.Options;

namespace Alphractal.Fees.Api.Repositories;

/// <summary>
/// Escreve o spool NDJSON em <c>pending/</c> e move para <c>ready/</c> ao fechar
/// o lote. Fora do caminho critico (RNF-25).
/// </summary>
/// <remarks>
/// Duas garantias importam aqui:
/// <list type="number">
/// <item>O ETL so enxerga arquivo COMPLETO. Enquanto o lote esta aberto ele vive
/// em <c>pending/</c>, que o Python nao varre; o move para <c>ready/</c> e
/// atomico no mesmo volume. Sem isso o ETL leria meio arquivo e o rejeitaria
/// inteiro por erro de contrato.</item>
/// <item>Falha de escrita NUNCA propaga para a ingestao. Perder um lote de spool
/// custa historico; travar a ingestao custa o painel ao vivo, que e o projeto.</item>
/// </list>
/// <para>
/// Os nomes de campo abaixo sao o contrato executavel de
/// <c>etl/src/alphractal_etl/contract.py</c>: campo a mais, a menos ou renomeado
/// manda o arquivo inteiro para <c>failed/</c>. Wei vai como numero JSON cru, sem
/// passar por <c>double</c> nem por <c>ulong</c> — <c>burned_wei</c> e
/// <c>total_fee_wei</c> podem exceder 2^64 em pico e a coluna e <c>UInt128</c>.
/// </para>
/// </remarks>
public sealed class NdjsonSpoolWriter : ISpoolWriter
{
    private readonly FeesOptions _options;
    private readonly ILogger<NdjsonSpoolWriter> _logger;
    private readonly SemaphoreSlim _gate = new(1, 1);

    private readonly DirectoryInfo _pending;
    private readonly DirectoryInfo _ready;

    private StringBuilder _buffer = new();
    private DateTimeOffset _batchStartedAt = DateTimeOffset.UtcNow;
    private int _lineCount;

    public NdjsonSpoolWriter(IOptions<FeesOptions> options, ILogger<NdjsonSpoolWriter> logger)
    {
        _options = options.Value;
        _logger = logger;

        var root = Path.GetFullPath(_options.SpoolPath);
        _pending = Directory.CreateDirectory(Path.Combine(root, "pending"));
        _ready = Directory.CreateDirectory(Path.Combine(root, "ready"));

        _logger.LogInformation("Spool em {Root} (pending -> ready).", root);
    }

    public async Task WriteBlockAsync(
        ChainBlockHeader block,
        BigInteger nextBaseFee,
        PriorityFeeSample tiers,
        uint transactionCount,
        BigInteger totalFeeWei,
        IReadOnlyList<FeeEstimate> estimates,
        EthPrice price,
        CancellationToken cancellationToken)
    {
        // Sem cotacao nao ha o que gravar: eth_usd e obrigatorio no contrato do
        // ETL e gravar zero corromperia as metricas financeiras.
        if (!price.HasValue)
        {
            _logger.LogDebug("Bloco {Number} nao foi para o spool: sem cotacao ETH/USD.", block.Number);
            return;
        }

        var usd = price.Price.ToString(CultureInfo.InvariantCulture);
        var lines = new List<string>(estimates.Count + 2);

        lines.Add(Line("eth_blocks", writer =>
        {
            Raw(writer, "block_number", block.Number);
            writer.WriteString("block_hash", block.Hash);
            writer.WriteString("block_timestamp", Iso(block.Timestamp));
            Raw(writer, "base_fee_per_gas", block.BaseFeePerGas);
            Raw(writer, "next_base_fee", nextBaseFee);
            Raw(writer, "gas_used", block.GasUsed);
            Raw(writer, "gas_limit", block.GasLimit);
            writer.WriteNumber("tx_count", transactionCount);
            Raw(writer, "priority_fee_p10", tiers.Slow);
            Raw(writer, "priority_fee_p50", tiers.Standard);
            Raw(writer, "priority_fee_p90", tiers.Fast);
            Raw(writer, "burned_wei", block.BaseFeePerGas * block.GasUsed);
            // Zero quando os recibos nao vieram: o contrato do ETL aceita a
            // coluna ausente ou zerada, e o painel cai na estimativa antiga em
            // vez de exibir um total falso.
            Raw(writer, "total_fee_wei", totalFeeWei);
            writer.WriteString("eth_usd", usd);
        }));

        foreach (var estimate in estimates)
        {
            var current = estimate;
            lines.Add(Line("fee_estimates", writer =>
            {
                writer.WriteString("sampled_at", Iso(block.ReceivedAt));
                Raw(writer, "block_number", block.Number);
                writer.WriteString("operation", current.Operation);
                writer.WriteString("speed", SpeedName(current.Speed));
                writer.WriteNumber("gas_units", current.GasUnits);
                Raw(writer, "total_fee_wei", current.TotalFeeWei);
                writer.WriteString("total_fee_gwei", FeeCalculatorGwei(current.TotalFeeWei));
                writer.WriteString(
                    "total_fee_usd",
                    Services.FeeCalculator.ToUsd(current.TotalFeeWei, price.Price)
                        .ToString(CultureInfo.InvariantCulture));
            }));
        }

        lines.Add(Line("eth_usd_prices", writer =>
        {
            writer.WriteString("observed_at", Iso(price.ObservedAt));
            writer.WriteString("source", price.Source);
            writer.WriteString("price_usd", usd);
        }));

        await AppendAsync(lines, cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteMempoolSampleAsync(
        DateTimeOffset sampledAt,
        BigInteger blockNumber,
        uint pendingTxCount,
        BigInteger baseFeePerGas,
        PriorityFeeSample tiers,
        EthPrice price,
        CancellationToken cancellationToken)
    {
        if (!price.HasValue)
        {
            return;
        }

        var line = Line("mempool_samples", writer =>
        {
            writer.WriteString("sampled_at", Iso(sampledAt));
            Raw(writer, "block_number", blockNumber);
            writer.WriteNumber("pending_tx_count", pendingTxCount);
            Raw(writer, "base_fee_per_gas", baseFeePerGas);
            Raw(writer, "suggested_priority_slow", tiers.Slow);
            Raw(writer, "suggested_priority_standard", tiers.Standard);
            Raw(writer, "suggested_priority_fast", tiers.Fast);
            writer.WriteString("eth_usd", price.Price.ToString(CultureInfo.InvariantCulture));
        });

        await AppendAsync([line], cancellationToken).ConfigureAwait(false);
    }

    public async Task WriteHealthAsync(
        string component,
        string status,
        long lagMs,
        BigInteger lastBlock,
        string detail,
        CancellationToken cancellationToken)
    {
        var line = Line("ingestion_health", writer =>
        {
            writer.WriteString("observed_at", Iso(DateTimeOffset.UtcNow));
            writer.WriteString("component", component);
            writer.WriteString("status", status);
            // O contrato do ETL rejeita inteiro unsigned negativo; um relogio
            // fora de sincronia produziria latencia negativa e mandaria o
            // arquivo INTEIRO para failed/ — levando os blocos junto.
            writer.WriteNumber("lag_ms", Math.Max(0, lagMs));
            Raw(writer, "last_block", BigInteger.Max(lastBlock, BigInteger.Zero));
            writer.WriteString("detail", detail.Length > 1000 ? detail[..1000] : detail);
        });

        await AppendAsync([line], cancellationToken).ConfigureAwait(false);
    }

    /// <summary>Monta uma linha NDJSON no formato <c>{"table":...,"data":{...}}</c>.</summary>
    /// <remarks>
    /// <see cref="Utf8JsonWriter"/> e nao interpolacao de string: ele escapa hash
    /// e nomes de operacao sozinho, e <c>WriteRawValue</c> deixa o
    /// <see cref="BigInteger"/> sair como numero cru — sem passar por
    /// <c>double</c> nem por <c>ulong</c>. <c>burned_wei</c> pode exceder 2^64 em
    /// pico e a coluna e <c>UInt128</c>; o <c>int()</c> do Python nao tem limite.
    /// </remarks>
    private static string Line(string table, Action<Utf8JsonWriter> writeData)
    {
        using var buffer = new MemoryStream(512);

        using (var writer = new Utf8JsonWriter(buffer))
        {
            writer.WriteStartObject();
            writer.WriteString("table", table);
            writer.WritePropertyName("data");
            writer.WriteStartObject();
            writeData(writer);
            writer.WriteEndObject();
            writer.WriteEndObject();
        }

        return Encoding.UTF8.GetString(buffer.ToArray());
    }

    private static void Raw(Utf8JsonWriter writer, string name, BigInteger value)
    {
        writer.WritePropertyName(name);
        writer.WriteRawValue(value.ToString(CultureInfo.InvariantCulture), skipInputValidation: true);
    }

    public async Task FlushAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await FlushLockedAsync().ConfigureAwait(false);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async ValueTask DisposeAsync()
    {
        // Encerramento limpo nao pode deixar lote pendente no disco.
        try
        {
            await FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Falha ao fechar o spool no encerramento.");
        }

        _gate.Dispose();
    }

    private async Task AppendAsync(IReadOnlyList<string> lines, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            foreach (var line in lines)
            {
                _buffer.Append(line.Trim()).Append('\n');
                _lineCount++;
            }

            var elapsed = DateTimeOffset.UtcNow - _batchStartedAt;
            if (elapsed.TotalMinutes >= _options.SpoolRotationMinutes)
            {
                await FlushLockedAsync().ConfigureAwait(false);
            }
        }
        finally
        {
            _gate.Release();
        }
    }

    private async Task FlushLockedAsync()
    {
        if (_lineCount == 0)
        {
            _batchStartedAt = DateTimeOffset.UtcNow;
            return;
        }

        var name = $"blocks-{DateTimeOffset.UtcNow:yyyyMMdd-HHmmss-fff}.ndjson";
        var pendingPath = Path.Combine(_pending.FullName, name);
        var readyPath = Path.Combine(_ready.FullName, name);
        var lines = _lineCount;

        try
        {
            await File.WriteAllTextAsync(pendingPath, _buffer.ToString(), new UTF8Encoding(false)).ConfigureAwait(false);
            File.Move(pendingPath, readyPath, overwrite: false);
            _logger.LogInformation("Lote de spool fechado: {Name} ({Lines} linhas).", name, lines);
        }
        catch (Exception exception)
        {
            // Deliberado: o caminho quente segue mesmo se o disco falhar.
            _logger.LogError(exception, "Falha ao gravar lote de spool {Name}. Lote descartado.", name);
            TryDelete(pendingPath);
        }
        finally
        {
            _buffer = new StringBuilder();
            _lineCount = 0;
            _batchStartedAt = DateTimeOffset.UtcNow;
        }
    }

    private void TryDelete(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (Exception exception)
        {
            _logger.LogWarning(exception, "Nao foi possivel remover o lote parcial {Path}.", path);
        }
    }

    private static string FeeCalculatorGwei(BigInteger wei)
        => Services.FeeCalculator.ToGwei(wei).ToString(CultureInfo.InvariantCulture);

    /// <summary>ISO-8601 com timezone — o contrato do ETL rejeita timestamp sem fuso.</summary>
    private static string Iso(DateTimeOffset value)
        => value.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    private static string SpeedName(SpeedTier tier) => tier switch
    {
        SpeedTier.Slow => "slow",
        SpeedTier.Fast => "fast",
        _ => "standard",
    };

}
