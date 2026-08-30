using Alphractal.Fees.Api.Configuration;
using Alphractal.Fees.Api.Providers;
using Alphractal.Fees.Api.Repositories;
using Alphractal.Fees.Api.Services;
using Microsoft.Extensions.Options;

namespace Alphractal.Fees.Api.BackgroundServices;

/// <summary>
/// Amostragem sub-bloco do mempool. Alimenta <c>mempool_samples</c> pelo spool.
/// </summary>
/// <remarks>
/// Serviço separado do <see cref="BlockIngestionService"/> de proposito: o valor
/// desta amostra e mostrar movimento ENTRE blocos. Amarrada ao ciclo do bloco,
/// ela teria exatamente a mesma frequencia de tudo o mais e nao acrescentaria
/// informacao nenhuma.
/// <para>
/// Nao alimenta o caminho quente. A pressao de mempool entra no painel pela
/// consulta ao caminho frio (<c>v_mempool_now</c>), com frescor de ~1 min — o
/// suficiente para uma tendencia, e sem gastar o orcamento do RNF-01.
/// </para>
/// <para>
/// Custo em RPC: uma chamada por amostra. No padrao de 4 s sao 15 por minuto,
/// contra 5 blocos/min da ingestao. E a peca mais cara do orcamento
/// (docs/requisitos/08) e a primeira a cortar se ele apertar — basta
/// <c>Fees:MempoolSampleSeconds = 0</c>.
/// </para>
/// </remarks>
public sealed class MempoolSamplingService : BackgroundService
{
    private readonly IChainMetricsProvider _metrics;
    private readonly IEthPriceProvider _price;
    private readonly HotBlockWindow _window;
    private readonly PriorityFeeState _tiers;
    private readonly ISpoolWriter _spool;
    private readonly FeesOptions _options;
    private readonly ILogger<MempoolSamplingService> _logger;

    public MempoolSamplingService(
        IChainMetricsProvider metrics,
        IEthPriceProvider price,
        HotBlockWindow window,
        PriorityFeeState tiers,
        ISpoolWriter spool,
        IOptions<FeesOptions> options,
        ILogger<MempoolSamplingService> logger)
    {
        _metrics = metrics;
        _price = price;
        _window = window;
        _tiers = tiers;
        _spool = spool;
        _options = options.Value;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (_options.MempoolSampleSeconds <= 0)
        {
            _logger.LogInformation("Amostragem de mempool desligada (Fees:MempoolSampleSeconds = 0).");
            return;
        }

        if (string.IsNullOrWhiteSpace(_options.RpcHttpUrl))
        {
            _logger.LogWarning("Amostragem de mempool desligada: Fees:RpcHttpUrl nao configurada.");
            return;
        }

        var period = TimeSpan.FromSeconds(_options.MempoolSampleSeconds);
        _logger.LogInformation("Amostragem de mempool a cada {Seconds}s.", _options.MempoolSampleSeconds);

        using var timer = new PeriodicTimer(period);

        while (await SafeWaitAsync(timer, stoppingToken).ConfigureAwait(false))
        {
            await SampleOnceAsync(stoppingToken).ConfigureAwait(false);
        }
    }

    private async Task SampleOnceAsync(CancellationToken cancellationToken)
    {
        try
        {
            var block = _window.Latest;

            // Sem bloco na janela nao ha base fee para associar a amostra, e sem
            // faixas a linha sairia com zeros que o painel exibiria como preco.
            // Esperar o proximo ciclo custa 4 s; gravar lixo custa a metrica.
            if (block is null || _tiers.IsEmpty)
            {
                return;
            }

            var pending = await _metrics.GetPendingTransactionCountAsync(cancellationToken).ConfigureAwait(false);
            if (pending == 0)
            {
                _logger.LogDebug("Mempool reportou zero pendentes; amostra descartada.");
                return;
            }

            var price = await _price.GetAsync(cancellationToken).ConfigureAwait(false);

            await _spool.WriteMempoolSampleAsync(
                DateTimeOffset.UtcNow,
                block.Number,
                pending,
                block.BaseFeePerGas,
                _tiers.Current,
                price,
                cancellationToken).ConfigureAwait(false);

            _logger.LogDebug("Mempool: {Pending} pendentes no bloco {Block}.", pending, block.Number);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // Falha aqui nao pode derrubar nada: mempool e informacao acessoria.
            _logger.LogWarning(exception, "Falha ao amostrar o mempool.");
        }
    }

    private static async Task<bool> SafeWaitAsync(PeriodicTimer timer, CancellationToken cancellationToken)
    {
        try
        {
            return await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }
}
