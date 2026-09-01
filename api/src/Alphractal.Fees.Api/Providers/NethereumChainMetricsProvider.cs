using System.Numerics;
using Alphractal.Fees.Api.Configuration;
using Alphractal.Fees.Api.Models.Domain;
using Microsoft.Extensions.Options;
using Nethereum.Hex.HexTypes;
using Nethereum.JsonRpc.Client;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.Web3;
using Newtonsoft.Json.Linq;

namespace Alphractal.Fees.Api.Providers;

/// <summary>
/// <c>eth_feeHistory</c>, <c>eth_getBlockReceipts</c> e contagem de transacoes por HTTP.
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

    public async Task<BlockFeeTotals?> GetBlockFeeTotalsAsync(
        BigInteger blockNumber,
        CancellationToken cancellationToken)
    {
        if (_web3 is null)
        {
            return null;
        }

        try
        {
            // Chamada crua em vez de um DTO da Nethereum: a 6.1.0 nao expoe
            // eth_getBlockReceipts, e so precisamos de dois campos por recibo.
            // JArray evita desserializar os logs, que sao a maior parte do
            // payload e nao entram em nenhuma conta.
            var request = new RpcRequest(
                Guid.NewGuid().ToString(),
                "eth_getBlockReceipts",
                new HexBigInteger(blockNumber).HexValue);

            var receipts = await _web3.Client
                .SendRequestAsync<JArray>(request)
                .ConfigureAwait(false);

            cancellationToken.ThrowIfCancellationRequested();

            if (receipts is null)
            {
                _logger.LogWarning("eth_getBlockReceipts nao devolveu recibos para o bloco {Block}.", blockNumber);
                return null;
            }

            var total = BigInteger.Zero;
            uint count = 0;

            foreach (var receipt in receipts)
            {
                var gasUsed = receipt["gasUsed"]?.Value<string>();
                var effectiveGasPrice = receipt["effectiveGasPrice"]?.Value<string>();

                // Recibo sem os dois campos so aconteceria em rede pre-London.
                // Devolver null e melhor que somar zero: um total parcial entra
                // no rollup indistinguivel de um total correto, e volta a
                // subestimar as taxas em silencio.
                if (gasUsed is null || effectiveGasPrice is null)
                {
                    _logger.LogWarning(
                        "Recibo sem gasUsed/effectiveGasPrice no bloco {Block}. Total descartado.",
                        blockNumber);
                    return null;
                }

                total += new HexBigInteger(gasUsed).Value * new HexBigInteger(effectiveGasPrice).Value;
                count++;
            }

            return new BlockFeeTotals { TotalFeeWei = total, TransactionCount = count };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            _logger.LogWarning(
                exception,
                "eth_getBlockReceipts falhou no bloco {Block}. O bloco vai para o spool sem total_fee_wei.",
                blockNumber);
            return null;
        }
    }

    public async Task<uint> GetPendingTransactionCountAsync(CancellationToken cancellationToken)
    {
        if (_web3 is null)
        {
            return 0;
        }

        var count = await _web3.Eth.Blocks.GetBlockTransactionCountByNumber
            .SendRequestAsync(BlockParameter.CreatePending())
            .ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        return (uint)count.Value;
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
