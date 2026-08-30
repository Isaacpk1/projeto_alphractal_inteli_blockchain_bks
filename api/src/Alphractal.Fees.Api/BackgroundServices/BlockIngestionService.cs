using System.Numerics;
using Alphractal.Fees.Api.Configuration;
using Alphractal.Fees.Api.Models.Domain;
using Alphractal.Fees.Api.Providers;
using Alphractal.Fees.Api.Repositories;
using Alphractal.Fees.Api.Services;
using Microsoft.Extensions.Options;

namespace Alphractal.Fees.Api.BackgroundServices;

/// <summary>
/// Ingestao continua: recebe o bloco, calcula, publica no SSE e grava no spool.
/// E o caminho quente inteiro.
/// </summary>
/// <remarks>
/// Ordem deliberada: PUBLICAR antes de gravar no spool. O painel e o requisito de
/// 2 segundos (RNF-01); o spool alimenta o historico, onde latencia de minutos e
/// aceitavel. Inverter a ordem colocaria I/O de disco dentro do orcamento do
/// tempo real.
/// <para>
/// Sem <c>Fees:RpcWebSocketUrl</c> este servico registra aviso e encerra, SEM
/// derrubar a aplicacao — a linha de corte do MVP exige que o caminho frio suba
/// sozinho (docs/requisitos/09 secao 4).
/// </para>
/// </remarks>
public sealed class BlockIngestionService : BackgroundService
{
    private readonly INewBlockProvider _blocks;
    private readonly IChainMetricsProvider _metrics;
    private readonly IEthPriceProvider _price;
    private readonly HotBlockWindow _window;
    private readonly FeeCalculator _calculator;
    private readonly SnapshotBuilder _builder;
    private readonly FeesBroadcaster _broadcaster;
    private readonly ISpoolWriter _spool;
    private readonly FeesOptions _options;
    private readonly PriorityFeeState _tiers;
    private readonly ILogger<BlockIngestionService> _logger;

    public BlockIngestionService(
        INewBlockProvider blocks,
        IChainMetricsProvider metrics,
        IEthPriceProvider price,
        HotBlockWindow window,
        FeeCalculator calculator,
        SnapshotBuilder builder,
        FeesBroadcaster broadcaster,
        ISpoolWriter spool,
        PriorityFeeState tiers,
        IOptions<FeesOptions> options,
        ILogger<BlockIngestionService> logger)
    {
        _blocks = blocks;
        _metrics = metrics;
        _price = price;
        _window = window;
        _calculator = calculator;
        _builder = builder;
        _broadcaster = broadcaster;
        _spool = spool;
        _tiers = tiers;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (string.IsNullOrWhiteSpace(_options.RpcWebSocketUrl))
        {
            _logger.LogWarning(
                "Ingestao desligada: Fees:RpcWebSocketUrl nao configurada. A API sobe assim mesmo e " +
                "serve o caminho frio. Configure com: " +
                "dotnet user-secrets set \"Fees:RpcWebSocketUrl\" \"wss://...\"");
            return;
        }

        _logger.LogInformation(
            "Ingestao iniciada. Janela {Window} blocos | N_fee {Fee} | N_cong {Cong}.",
            _options.HotWindowBlocks,
            _options.FeeWindowBlocks,
            _options.CongestionWindowBlocks);

        try
        {
            await _blocks.RunAsync(HandleBlockAsync, stoppingToken).ConfigureAwait(false);
        }
        finally
        {
            await _spool.FlushAsync(CancellationToken.None).ConfigureAwait(false);
        }
    }

    private async Task HandleBlockAsync(ChainBlockHeader block, CancellationToken cancellationToken)
    {
        var reorg = _window.Add(block);
        if (reorg)
        {
            _logger.LogWarning("Reorg: bloco {Number} substituiu o ramo anterior.", block.Number);
        }

        var tiers = await ResolveTiersAsync(block.Number, cancellationToken).ConfigureAwait(false);
        var price = await _price.GetAsync(cancellationToken).ConfigureAwait(false);

        var recentBaseFees = _window
            .Snapshot(_options.CongestionWindowBlocks)
            .Reverse()
            .Select(static header => header.BaseFeePerGas)
            .ToList();

        var snapshot = _builder.Build(
            block, tiers, recentBaseFees, price, _window.Count, DateTimeOffset.UtcNow);

        // 1) Painel primeiro: e o orcamento de 2 s (RNF-01).
        _broadcaster.Publish(snapshot);

        _logger.LogInformation(
            "Bloco {Number} | {BaseFee} gwei | {Congestion} | gas {Ratio:P1} | latencia {Latency:0.0}s | {Subs} assinante(s)",
            block.Number,
            snapshot.BaseFeeGwei.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture),
            snapshot.Congestion.Level,
            snapshot.GasUsedRatio,
            snapshot.DeliveryLatencySeconds,
            _broadcaster.SubscriberCount);

        // 2) Spool depois, e sem poder derrubar a ingestao.
        await WriteSpoolSafelyAsync(block, tiers, price, cancellationToken).ConfigureAwait(false);
    }

    private async Task<PriorityFeeSample> ResolveTiersAsync(BigInteger blockNumber, CancellationToken cancellationToken)
    {
        try
        {
            var history = await _metrics
                .GetPriorityFeeHistoryAsync(blockNumber, _options.FeeWindowBlocks, cancellationToken)
                .ConfigureAwait(false);

            if (history.Count > 0)
            {
                _tiers.Current = _calculator.SpeedTiers(history);
            }
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Degrada: mantem as ultimas faixas conhecidas em vez de zerar o
            // painel por uma falha de uma chamada HTTP.
            _logger.LogWarning(exception, "eth_feeHistory falhou no bloco {Number}. Mantendo faixas anteriores.", blockNumber);
        }

        return _tiers.Current;
    }

    private async Task WriteSpoolSafelyAsync(
        ChainBlockHeader block,
        PriorityFeeSample tiers,
        EthPrice price,
        CancellationToken cancellationToken)
    {
        try
        {
            var txCount = await _metrics.GetTransactionCountAsync(block.Number, cancellationToken).ConfigureAwait(false);
            var nextBaseFee = FeeCalculator.NextBaseFee(block.BaseFeePerGas, block.GasUsed, block.GasLimit);
            var estimates = _calculator.EstimateAll(block.BaseFeePerGas, tiers);

            await _spool
                .WriteBlockAsync(block, nextBaseFee, tiers, txCount, estimates, price, cancellationToken)
                .ConfigureAwait(false);

            // Heartbeat real, substituindo os valores de seed que o /status
            // exibia. Dois componentes distintos porque falham de formas
            // diferentes: o socket pode cair com o processo vivo.
            await _spool.WriteHealthAsync(
                "ws_listener",
                "ok",
                (long)block.DeliveryLatency.TotalMilliseconds,
                block.Number,
                $"newHeads; janela {_window.Count}/{_options.HotWindowBlocks}",
                cancellationToken).ConfigureAwait(false);

            await _spool.WriteHealthAsync(
                "api",
                price.HasValue ? "ok" : "degraded",
                0,
                block.Number,
                price.HasValue
                    ? $"sse={_broadcaster.SubscriberCount}; preco={price.Source}"
                    : $"sse={_broadcaster.SubscriberCount}; sem cotacao ETH/USD",
                cancellationToken).ConfigureAwait(false);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogError(exception, "Falha ao gravar bloco {Number} no spool. Caminho quente segue.", block.Number);
        }
    }
}
