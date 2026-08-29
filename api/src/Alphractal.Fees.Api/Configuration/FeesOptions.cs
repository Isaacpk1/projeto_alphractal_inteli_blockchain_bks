using System.ComponentModel.DataAnnotations;

namespace Alphractal.Fees.Api.Configuration;

/// <summary>
/// Parametros do caminho quente: ingestao RPC, janelas, faixas e spool.
/// </summary>
/// <remarks>
/// Percentis, janelas, faixas de congestionamento e gas limits estao AQUI e nao
/// no codigo porque a spec os define como configuraveis (RN-02, RN-04, RN-11) —
/// o parceiro deu liberdade nas faixas e sinalizou que pode mandar valores
/// proprios depois. Trocar um numero desses tem de ser edicao de JSON, nao PR.
/// </remarks>
public sealed class FeesOptions
{
    public const string SectionName = "Fees";

    /// <summary>
    /// Endpoint WebSocket da Alchemy. Vem de user-secrets ou variavel de ambiente,
    /// nunca do appsettings.
    /// </summary>
    /// <remarks>
    /// NAO e <c>[Required]</c> de proposito. A linha de corte do MVP (doc 09
    /// secao 4) exige que os dois caminhos subam separados: exigir a chave no
    /// boot impediria rodar so o caminho frio. Quem valida e o
    /// <c>BlockIngestionService</c>, que registra aviso e encerra a ingestao se
    /// estiver vazia — sem derrubar a aplicacao.
    /// </remarks>
    public string RpcWebSocketUrl { get; init; } = string.Empty;

    /// <summary>
    /// Endpoint HTTP da Alchemy, para <c>eth_feeHistory</c> e <c>eth_getBlockByNumber</c>.
    /// Mesma chave do WebSocket, protocolo diferente.
    /// </summary>
    /// <remarks>
    /// O <c>newHeads</c> entrega so o cabecalho: nao traz priority fee nem
    /// contagem de transacoes. As faixas de velocidade (RN-02) exigem
    /// <c>eth_feeHistory</c>, que so existe por HTTP. Sao 2 chamadas extras por
    /// bloco — contabilizadas no orcamento de Compute Units (docs/requisitos/08).
    /// </remarks>
    public string RpcHttpUrl { get; init; } = string.Empty;

    /// <summary>
    /// Cotacao ETH/USD usada quando o provider de preco nao responde.
    /// Zero desliga a estimativa em USD em vez de publicar preco inventado.
    /// </summary>
    /// <remarks>
    /// Gravar preco zero ou chutado corromperia as metricas financeiras — a mesma
    /// razao pela qual o backfill do ETL exige <c>--eth-usd</c> explicito.
    /// </remarks>
    [Range(0, 1_000_000)]
    public decimal FallbackEthUsd { get; init; }

    /// <summary>URL da cotacao ETH/USD. Vazio usa apenas <see cref="FallbackEthUsd"/>.</summary>
    /// <remarks>
    /// Padrao e a Coinbase, que responde sem chave. O CoinGecko passou a exigir
    /// chave ate no endpoint publico e devolve 403 — se voltar a usa-lo, ajuste
    /// tambem <see cref="PriceJsonPath"/> para <c>ethereum.usd</c>.
    /// </remarks>
    public string PriceSourceUrl { get; init; } =
        "https://api.coinbase.com/v2/prices/ETH-USD/spot";

    /// <summary>
    /// Caminho do valor dentro da resposta JSON, com pontos separando os niveis.
    /// </summary>
    /// <remarks>
    /// Fonte e caminho andam juntos: trocar de provedor de cotacao vira duas
    /// linhas de configuracao em vez de um deploy. Aceita o valor como numero ou
    /// como string — a Coinbase devolve <c>"3200.00"</c> entre aspas.
    /// </remarks>
    public string PriceJsonPath { get; init; } = "data.amount";

    /// <summary>Tamanho da janela quente em blocos — <c>N_buffer</c> (RN-10).</summary>
    [Range(1, 5_000)]
    public int HotWindowBlocks { get; init; } = 300;

    /// <summary>
    /// <c>N_fee</c> (RN-02): blocos usados para os percentis de priority fee.
    /// 20 blocos ≈ 4 min — responde rapido sem oscilar a cada bloco isolado.
    /// </summary>
    [Range(1, 1_000)]
    public int FeeWindowBlocks { get; init; } = 20;

    /// <summary>
    /// <c>N_cong</c> (RN-04): blocos da media movel de congestionamento.
    /// 100 blocos ≈ 20 min.
    /// </summary>
    [Range(1, 5_000)]
    public int CongestionWindowBlocks { get; init; } = 100;

    /// <summary>
    /// Percentis de priority fee pedidos ao <c>eth_feeHistory</c>, na ordem
    /// lento / padrao / rapido (RN-02).
    /// </summary>
    /// <remarks>
    /// O lento e p10 e nao p25: e o valor que o schema do ClickHouse, o contrato
    /// do ETL e o backfill ja usam. Definicao do time, revisavel — se a
    /// Alphractal pedir outro percentil, muda aqui e na coluna, nada mais.
    /// </remarks>
    public FeePercentiles Percentiles { get; init; } = new();

    /// <summary>Faixas do indice de congestionamento (RN-04).</summary>
    public CongestionThresholds Congestion { get; init; } = new();

    /// <summary>
    /// Sem bloco novo por mais que isto, o painel sai de "Ao vivo" (RN-07).
    /// 60 s ≈ 5 blocos.
    /// </summary>
    [Range(5, 3_600)]
    public int StaleAfterSeconds { get; init; } = 60;

    /// <summary>
    /// Periodo da amostragem de mempool, em segundos. <c>0</c> desliga.
    /// </summary>
    /// <remarks>
    /// 4 s da ~3 amostras por bloco — suficiente para ver pressao se acumulando
    /// entre blocos. E a peca mais cara do orcamento de RPC (uma chamada por
    /// amostra, contra 5 blocos/min da ingestao) e a primeira a cortar se ele
    /// apertar.
    /// </remarks>
    [Range(0, 3_600)]
    public int MempoolSampleSeconds { get; init; } = 4;

    /// <summary>Intervalo de atualizacao da cotacao ETH/USD (RN-03).</summary>
    [Range(5, 3_600)]
    public int PriceRefreshSeconds { get; init; } = 60;

    /// <summary>Acima disto, o valor em USD e exibido como desatualizado (RN-03).</summary>
    [Range(10, 86_400)]
    public int PriceStaleAfterSeconds { get; init; } = 300;

    /// <summary>
    /// Gas limits de referencia por tipo de operacao (RN-11). Ajustaveis quando a
    /// Alphractal enviar as operacoes que o usuario institucional deles executa.
    /// </summary>
    public Dictionary<string, uint> GasLimits { get; init; } = new(StringComparer.OrdinalIgnoreCase)
    {
        ["transfer"] = 21_000,
        ["erc20_transfer"] = 65_000,
        ["approve"] = 46_000,
        ["uniswap_v3_swap"] = 150_000,
        ["nft_mint"] = 85_000,
    };

    /// <summary>Diretorio raiz do spool NDJSON consumido pelo ETL.</summary>
    [Required]
    public required string SpoolPath { get; init; }

    /// <summary>Minutos por arquivo de spool antes de mover para ready/.</summary>
    [Range(1, 60)]
    public int SpoolRotationMinutes { get; init; } = 1;
}

/// <summary>Percentis de priority fee, em pontos percentuais (RN-02).</summary>
public sealed class FeePercentiles
{
    [Range(0, 100)] public double Slow { get; init; } = 10;
    [Range(0, 100)] public double Standard { get; init; } = 50;
    [Range(0, 100)] public double Fast { get; init; } = 90;
}

/// <summary>
/// Limiares do indice de congestionamento, como razao entre a base fee atual e a
/// media movel de <c>N_cong</c> blocos (RN-04).
/// </summary>
/// <remarks>
/// Esta regra mede VARIACAO, nao NIVEL: num periodo sustentado de taxas altas a
/// media movel acompanha a subida e o indicador volta a marcar "Normal". O ponto
/// cego e conhecido e o D-02 (percentil historico de 30 dias) existe para cobri-lo.
/// </remarks>
public sealed class CongestionThresholds
{
    /// <summary>Abaixo disto: Baixo.</summary>
    [Range(0.01, 10)] public double Low { get; init; } = 0.70;

    /// <summary>A partir disto: Alto.</summary>
    [Range(0.01, 10)] public double High { get; init; } = 1.30;

    /// <summary>A partir disto: Extremo.</summary>
    [Range(0.01, 100)] public double Extreme { get; init; } = 2.00;
}
