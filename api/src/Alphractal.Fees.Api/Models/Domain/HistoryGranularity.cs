namespace Alphractal.Fees.Api.Models.Domain;

/// <summary>
/// Granularidade das series do caminho frio. Corresponde a coluna
/// <c>granularity</c> de <c>eth_fees_rollup</c> e escolhe entre as views
/// <c>v_eth_fees_1h</c> e <c>v_eth_fees_1d</c>.
/// </summary>
/// <remarks>
/// Enum, e nao string, de proposito: o nome da view nunca vem de entrada do
/// usuario, entao nao existe caminho de injecao na escolha da consulta.
/// </remarks>
public enum HistoryGranularity
{
    Hour = 0,
    Day = 1,
}
