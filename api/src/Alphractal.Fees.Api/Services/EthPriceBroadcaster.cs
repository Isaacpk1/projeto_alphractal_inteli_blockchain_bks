using System.Threading.Channels;
using Alphractal.Fees.Api.Providers;

namespace Alphractal.Fees.Api.Services;

/// <summary>Fan-out da ultima cotacao ETH/USD para os clientes SSE.</summary>
public sealed class EthPriceBroadcaster
{
    private readonly List<Channel<EthPrice>> _subscribers = [];
    private readonly object _gate = new();
    private EthPrice _latest = EthPrice.None;

    public void Publish(EthPrice price)
    {
        lock (_gate)
        {
            _latest = price;
            foreach (var subscriber in _subscribers)
            {
                subscriber.Writer.TryWrite(price);
            }
        }
    }

    public async IAsyncEnumerable<EthPrice> SubscribeAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<EthPrice>(new BoundedChannelOptions(1)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

        EthPrice initial;
        lock (_gate)
        {
            _subscribers.Add(channel);
            initial = _latest;
        }

        try
        {
            if (initial.HasValue)
            {
                yield return initial;
            }

            await foreach (var price in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return price;
            }
        }
        finally
        {
            lock (_gate)
            {
                _subscribers.Remove(channel);
            }

            channel.Writer.TryComplete();
        }
    }
}
