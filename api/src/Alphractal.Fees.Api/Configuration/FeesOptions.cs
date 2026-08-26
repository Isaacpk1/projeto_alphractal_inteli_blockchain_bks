using System.ComponentModel.DataAnnotations;

namespace Alphractal.Fees.Api.Configuration;

/// <summary>
/// Parametros do caminho quente: ingestao RPC, janela em memoria e spool.
/// </summary>
public sealed class FeesOptions
{
    public const string SectionName = "Fees";

    /// <summary>Endpoint WebSocket da Alchemy. Vem do .env, nunca do appsettings.</summary>
    [Required]
    public required string RpcWebSocketUrl { get; init; }

    /// <summary>Tamanho da janela quente em blocos (RN-10).</summary>
    [Range(1, 5_000)]
    public int HotWindowBlocks { get; init; } = 300;

    /// <summary>Diretorio raiz do spool NDJSON consumido pelo ETL.</summary>
    [Required]
    public required string SpoolPath { get; init; }

    /// <summary>Minutos por arquivo de spool antes de mover para ready/.</summary>
    [Range(1, 60)]
    public int SpoolRotationMinutes { get; init; } = 1;
}
