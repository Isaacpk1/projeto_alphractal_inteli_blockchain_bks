using System.ComponentModel.DataAnnotations;

namespace Alphractal.Fees.Api.Configuration;

/// <summary>
/// Conexao com o ClickHouse do caminho frio (interface HTTP nativa, porta 8123).
/// Usado apenas por consultas historicas — nunca pelo caminho quente (RN-14).
/// </summary>
public sealed class ClickHouseOptions
{
    public const string SectionName = "ClickHouse";

    [Required]
    public required string BaseUrl { get; init; }

    [Required]
    public required string Database { get; init; }

    [Required]
    public required string User { get; init; }

    public string Password { get; init; } = string.Empty;

    /// <summary>Timeout de consulta, em segundos.</summary>
    [Range(1, 120)]
    public int TimeoutSeconds { get; init; } = 10;
}
