using System.Threading.Channels;
using Alphractal.Fees.Api.Models.Responses;

namespace Alphractal.Fees.Api.Services;

/// <summary>
/// Fan-out de um produtor (a ingestao) para N assinantes SSE.
/// </summary>
/// <remarks>
/// Cada assinante tem o proprio <see cref="Channel{T}"/> limitado. Um navegador
/// lento, uma aba em segundo plano ou uma rede ruim NAO podem segurar a ingestao
/// — por isso o canal e <c>Bounded</c> com <c>DropOldest</c>: em painel ao vivo o
/// bloco mais novo substitui o antigo sem prejuizo, e a alternativa (canal
/// ilimitado) seria vazamento de memoria proporcional ao pior cliente.
/// <para>
/// O ultimo snapshot fica guardado para entregar na conexao (RN-13): sem isso o
/// painel ficaria em branco por ate 12 s a cada carregamento.
/// </para>
/// </remarks>
public sealed class FeesBroadcaster
{
    private const int PerSubscriberBuffer = 4;

    private readonly List<Channel<FeesSnapshotResponse>> _subscribers = [];
    private readonly object _gate = new();

    private FeesSnapshotResponse? _latest;

    /// <summary>Ultimo snapshot publicado, ou <c>null</c> se nenhum bloco chegou ainda.</summary>
    public FeesSnapshotResponse? Latest
    {
        get { lock (_gate) { return _latest; } }
    }

    public int SubscriberCount
    {
        get { lock (_gate) { return _subscribers.Count; } }
    }

    public void Publish(FeesSnapshotResponse snapshot)
    {
        lock (_gate)
        {
            _latest = snapshot;

            foreach (var subscriber in _subscribers)
            {
                // TryWrite nunca bloqueia: com DropOldest o canal cheio descarta o
                // item mais antigo e aceita o novo.
                subscriber.Writer.TryWrite(snapshot);
            }
        }
    }

    /// <summary>
    /// Fluxo de snapshots para um assinante. Entrega o ultimo conhecido primeiro
    /// (RN-13) e depois cada novo bloco, ate o cliente desconectar.
    /// </summary>
    public async IAsyncEnumerable<FeesSnapshotResponse> SubscribeAsync(
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<FeesSnapshotResponse>(new BoundedChannelOptions(PerSubscriberBuffer)
        {
            FullMode = BoundedChannelFullMode.DropOldest,
            SingleReader = true,
            SingleWriter = false,
        });

        FeesSnapshotResponse? initial;
        lock (_gate)
        {
            _subscribers.Add(channel);
            initial = _latest;
        }

        try
        {
            if (initial is not null)
            {
                yield return initial;
            }

            await foreach (var snapshot in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
            {
                yield return snapshot;
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
