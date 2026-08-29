using Alphractal.Fees.Api.Models.Domain;

namespace Alphractal.Fees.Api.Services;

/// <summary>
/// Ultimas faixas de priority fee calculadas (RN-02), compartilhadas entre a
/// ingestao de blocos e a amostragem de mempool.
/// </summary>
/// <remarks>
/// Existe porque as faixas nascem no ciclo do bloco (que roda a cada ~12 s) e sao
/// consumidas tambem pela amostragem sub-bloco (que roda com periodo proprio).
/// A alternativa seria a amostragem chamar <c>eth_feeHistory</c> por conta
/// propria — triplicando o consumo de Compute Units para reobter um valor que
/// nao muda entre blocos.
/// <para>
/// Registro de referencia: leitura e escrita de referencia sao atomicas em .NET,
/// e <see cref="PriorityFeeSample"/> e imutavel. <c>volatile</c> garante que o
/// leitor enxergue a escrita mais recente sem lock.
/// </para>
/// </remarks>
public sealed class PriorityFeeState
{
    private volatile PriorityFeeSample _current = new() { Slow = 0, Standard = 0, Fast = 0 };

    public PriorityFeeSample Current
    {
        get => _current;
        set => _current = value;
    }

    /// <summary>Ainda nao houve nenhuma leitura bem-sucedida de <c>eth_feeHistory</c>.</summary>
    public bool IsEmpty => _current.Slow.IsZero && _current.Standard.IsZero && _current.Fast.IsZero;
}
