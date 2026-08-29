using System.Numerics;

namespace Alphractal.Fees.Api.Models.Domain;

/// <summary>
/// Cabecalho de bloco como a rede entrega, antes de qualquer calculo.
/// </summary>
/// <remarks>
/// Wei e <see cref="BigInteger"/>, nunca <c>double</c> nem <c>long</c> (RN-06).
/// Uma base fee de 100 gwei vezes um gas limit de 36 milhoes ja passa de
/// 3,6e15 — ainda cabe em <c>long</c>, mas o produto por gas de um bloco cheio
/// em pico nao cabe com folga, e o erro seria silencioso. E o risco R-03.
/// <para>
/// <see cref="ReceivedAt"/> vem do relogio local e serve so para medir latencia
/// de entrega; <see cref="Timestamp"/> e o tempo do bloco segundo a rede.
/// </para>
/// </remarks>
public sealed record ChainBlockHeader
{
    public required BigInteger Number { get; init; }
    public required string Hash { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public required BigInteger BaseFeePerGas { get; init; }
    public required BigInteger GasUsed { get; init; }
    public required BigInteger GasLimit { get; init; }
    public required DateTimeOffset ReceivedAt { get; init; }

    /// <summary>Latencia entre o timestamp do bloco e a chegada aqui.</summary>
    public TimeSpan DeliveryLatency => ReceivedAt - Timestamp;
}
