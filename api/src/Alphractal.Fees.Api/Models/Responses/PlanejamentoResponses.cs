namespace Alphractal.Fees.Api.Models.Responses;

/// <summary>Media da base fee numa hora do dia (UTC), sobre 30 dias.</summary>
public sealed record HoraDoDiaResponse
{
    public required int HoraUtc { get; init; }
    public required ulong Amostras { get; init; }
    public required double BaseFeeGweiAvg { get; init; }
    public required double BaseFeeGweiP50 { get; init; }
    public required double BaseFeeGweiMin { get; init; }
    public required double BaseFeeGweiMax { get; init; }
}

/// <summary>Celula do heatmap. <c>diaSemana</c>: 1 = segunda … 7 = domingo.</summary>
public sealed record SemanaHoraResponse
{
    public required int DiaSemana { get; init; }
    public required int HoraUtc { get; init; }
    public required ulong Amostras { get; init; }
    public required double BaseFeeGweiAvg { get; init; }
}

/// <summary>
/// "Espero ou executo agora?" — a pergunta que o painel existe para responder.
/// </summary>
public sealed record RecomendacaoResponse
{
    public required double BaseFeeGweiAgora { get; init; }
    public required int MelhorHoraUtc { get; init; }
    public required double MelhorHoraGwei { get; init; }
    public required int PiorHoraUtc { get; init; }
    public required double PiorHoraGwei { get; init; }
    public required double MediaGeralGwei { get; init; }

    /// <summary>Economia % de esperar pela melhor hora. Negativa = agora esta melhor.</summary>
    public required double EconomiaPercentual { get; init; }

    public required int HorasDeEspera { get; init; }

    /// <summary>Historico curto demais para recomendar. Exiba junto, nao esconda.</summary>
    public required bool PoucaConfianca { get; init; }

    /// <summary>Frase pronta, para o painel nao reimplementar a regra (RN-09).</summary>
    public required string Resumo { get; init; }

    public required IReadOnlyList<HoraDoDiaResponse> Horas { get; init; }
}

/// <summary>Cotacao do ETH com a variacao de 24 h.</summary>
public sealed record EthUsd24hResponse
{
    public required decimal PrecoAtual { get; init; }
    public required DateTimeOffset ObservadoEmUtc { get; init; }

    /// <summary><c>null</c> quando nao ha cotacao de 24 h atras — diferente de "variou 0%".</summary>
    public decimal? Preco24h { get; init; }
    public double? VariacaoPercentual { get; init; }
}

/// <summary>Custo de uma transacao com gas limit informado pelo usuario.</summary>
public sealed record CustoPorGasResponse
{
    public required uint GasUnits { get; init; }
    public required ulong BlockNumber { get; init; }
    public required decimal BaseFeeGwei { get; init; }
    public required IReadOnlyList<OperationCostResponse> Custos { get; init; }
    public EthPriceResponse? EthUsd { get; init; }
}

/// <summary>Taxa de queima do EIP-1559 medida na janela quente.</summary>
/// <remarks>
/// A base fee e queimada pelo protocolo — some da oferta. E a metrica que
/// transforma congestionamento em impacto economico visivel.
/// </remarks>
public sealed record QueimaResponse
{
    public required decimal EthPorMinuto { get; init; }
    public required decimal EthNoUltimoBloco { get; init; }
    public required decimal EthNaJanela { get; init; }
    public required int BlocosNaJanela { get; init; }
    public required double MinutosDaJanela { get; init; }
    public decimal? UsdPorMinuto { get; init; }
}
