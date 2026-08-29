using System.Numerics;
using System.Threading.Channels;
using Alphractal.Fees.Api.Configuration;
using Alphractal.Fees.Api.Models.Domain;
using Microsoft.Extensions.Options;
using Nethereum.JsonRpc.WebSocketStreamingClient;
using Nethereum.RPC.Eth.DTOs;
using Nethereum.RPC.Reactive.Eth.Subscriptions;

namespace Alphractal.Fees.Api.Providers;

/// <summary>
/// Assina <c>newHeads</c> na Alchemy por WebSocket usando Nethereum.
/// </summary>
/// <remarks>
/// E a UNICA conexao RPC do projeto em regime normal: o ETL le o spool que a API
/// escreve, nao a rede. Duas conexoes para a mesma fonte dobrariam o consumo do
/// orcamento de Compute Units (docs/requisitos/08).
/// <para>
/// A subscricao da Nethereum e um <c>IObservable</c> (push, sincrono). O handler
/// aqui e assincrono, entao os blocos passam por um <see cref="Channel{T}"/>: o
/// callback so enfileira e o consumo acontece fora da thread do socket. Sem isso,
/// um handler lento seguraria o proprio WebSocket.
/// </para>
/// </remarks>
public sealed class NethereumNewBlockProvider : INewBlockProvider
{
    private static readonly TimeSpan MinBackoff = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan MaxBackoff = TimeSpan.FromSeconds(30);

    private readonly FeesOptions _options;
    private readonly ILogger<NethereumNewBlockProvider> _logger;

    public NethereumNewBlockProvider(IOptions<FeesOptions> options, ILogger<NethereumNewBlockProvider> logger)
    {
        _options = options.Value;
        _logger = logger;
    }

    public async Task RunAsync(
        Func<ChainBlockHeader, CancellationToken, Task> onBlock,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.RpcWebSocketUrl))
        {
            throw new InvalidOperationException(
                "Fees:RpcWebSocketUrl nao configurada. Defina por user-secrets ou variavel de ambiente " +
                "(Fees__RpcWebSocketUrl). A chave nunca vai para appsettings.json.");
        }

        var backoff = MinBackoff;

        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await ConsumeAsync(onBlock, cancellationToken).ConfigureAwait(false);
                backoff = MinBackoff; // uma sessao saudavel zera o backoff
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Conexao WebSocket caiu. Reconectando em {Backoff:0.#}s.",
                    backoff.TotalSeconds);
            }

            try
            {
                await Task.Delay(backoff, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }

            // Backoff exponencial com teto: em queda longa da Alchemy nao
            // martelamos o endpoint nem gastamos Compute Units a toa.
            backoff = TimeSpan.FromTicks(Math.Min(backoff.Ticks * 2, MaxBackoff.Ticks));
        }
    }

    private async Task ConsumeAsync(
        Func<ChainBlockHeader, CancellationToken, Task> onBlock,
        CancellationToken cancellationToken)
    {
        // Bounded: se o consumidor travar, preferimos descartar o bloco mais
        // antigo a crescer memoria sem limite. Em painel ao vivo, dado velho nao
        // tem valor — o mais novo tem.
        var channel = Channel.CreateBounded<ChainBlockHeader>(new BoundedChannelOptions(64)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleWriter = true,
            SingleReader = true,
        });

        using var client = new StreamingWebSocketClient(_options.RpcWebSocketUrl);
        var subscription = new EthNewBlockHeadersObservableSubscription(client);

        using var registration = subscription
            .GetSubscriptionDataResponsesAsObservable()
            .Subscribe(
                block =>
                {
                    var header = Map(block);
                    if (header is not null && !channel.Writer.TryWrite(header))
                    {
                        _logger.LogWarning("Bloco {Number} descartado: fila cheia.", header.Number);
                    }
                },
                error =>
                {
                    _logger.LogWarning(error, "Erro na subscricao newHeads.");
                    channel.Writer.TryComplete(error);
                },
                () => channel.Writer.TryComplete());

        await client.StartAsync().ConfigureAwait(false);
        await subscription.SubscribeAsync().ConfigureAwait(false);
        _logger.LogInformation("Assinado newHeads. Aguardando blocos.");

        await foreach (var header in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
        {
            await onBlock(header, cancellationToken).ConfigureAwait(false);
        }
    }

    private ChainBlockHeader? Map(Block block)
    {
        // Bloco sem numero e cabecalho pendente: a rede ainda nao o fixou.
        if (block.Number is null)
        {
            return null;
        }

        return new ChainBlockHeader
        {
            Number = block.Number.Value,
            Hash = block.BlockHash ?? string.Empty,
            Timestamp = DateTimeOffset.FromUnixTimeSeconds((long)block.Timestamp.Value),
            // Pre-EIP-1559 nao tem base fee. Nao acontece na mainnet de hoje, mas
            // um null aqui viraria NullReferenceException no meio da demo.
            BaseFeePerGas = block.BaseFeePerGas?.Value ?? BigInteger.Zero,
            GasUsed = block.GasUsed?.Value ?? BigInteger.Zero,
            GasLimit = block.GasLimit?.Value ?? BigInteger.Zero,
            ReceivedAt = DateTimeOffset.UtcNow,
        };
    }
}
