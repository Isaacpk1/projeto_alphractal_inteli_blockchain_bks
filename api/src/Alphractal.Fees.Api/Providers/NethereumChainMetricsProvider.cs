using System.Numerics;
using Alphractal.Fees.Api.Configuration;
using Alphractal.Fees.Api.Models.Domain;
using Microsoft.Extensions.Options;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;

namespace Alphractal.Fees.Api.Providers;

/// <summary>
/// <c>eth_feeHistory</c> e <c>eth_getBlockByNumber</c> por HTTP.
/// </summary>
/// <remarks>
/// Falha aqui NAO derruba a ingestao: sem faixas de velocidade o painel ainda
/// mostra base fee, congestionamento e projecao. Degradar e melhor que apagar.
/// </remarks>
public sealed class NethereumChainMetricsProvider : IChainMetricsProvider
{
    private readonly Web3? _web3;
    private readonly FeesOptions _options;
    private readonly ILogger<NethereumChainMetricsProvider> _logger;

    public NethereumChainMetricsProvider(
        IOptions<FeesOptions> options,
        ILogger<NethereumChainMetricsProvider> logger)
    {
        _options = options.Value;
        _logger = logger;

        // NAO lanca quando falta a URL: este provider e singleton e seria
        // construido tambem numa subida so do caminho frio, derrubando a
        // aplicacao inteira por falta de uma chave que aquele caminho nao usa.
        if (string.IsNullOrWhiteSpace(_options.RpcHttpUrl))
        {
            _logger.LogWarning(
                "Fees:RpcHttpUrl nao configurada. Faixas de velocidade (RN-02) e tx_count ficam " +
                "indisponiveis; base fee, congestionamento e projecao seguem funcionando.");
            _web3 = null;
        }
        else
        {
            _web3 = new Web3(_options.RpcHttpUrl);
        }
    }

    public async Task<IReadOnlyList<PriorityFeeSample>> GetPriorityFeeHistoryAsync(
        BigInteger newestBlock,
        int blockCount,
        CancellationToken cancellationToken)
    {
        if (_web3 is null)
        {
            return Array.Empty<PriorityFeeSample>();
        }

        var percentiles = _options.Percentiles;

        // A configuracao usa double (JSON nao tem decimal); a Nethereum pede
        // decimal[]. A conversao acontece aqui, no limite do adaptador — nao vale
        // contaminar FeesOptions com o tipo de uma biblioteca.
        var requested = new[]
        {
            (decimal)percentiles.Slow,
            (decimal)percentiles.Standard,
            (decimal)percentiles.Fast,
        };

        var history = await _web3.Eth.FeeHistory
            .SendRequestAsync(
                new HexBigInteger(blockCount),
                new BlockParameter(new HexBigInteger(newestBlock)),
                requested)
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        if (history?.Reward is null)
        {
            _logger.LogWarning("eth_feeHistory nao devolveu rewards para o bloco {Block}.", newestBlock);
            return Array.Empty<PriorityFeeSample>();
        }

        var samples = new List<PriorityFeeSample>(history.Reward.Length);

        foreach (var reward in history.Reward)
        {
            // Bloco vazio devolve array incompleto; descartar e melhor que
            // assumir zero, que puxaria a mediana das faixas para baixo.
            if (reward is null || reward.Length < 3)
            {
                continue;
            }

            samples.Add(new PriorityFeeSample
            {
                Slow = reward[0].Value,
                Standard = reward[1].Value,
                Fast = reward[2].Value,
            });
        }

        return samples;
    }

    public async Task<uint> GetTransactionCountAsync(BigInteger blockNumber, CancellationToken cancellationToken)
    {
        if (_web3 is null)
        {
            return 0;
        }

        var count = await _web3.Eth.Blocks.GetBlockTransactionCountByNumber
            .SendRequestAsync(new HexBigInteger(blockNumber))
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        return (uint)count.Value;
    }
}
