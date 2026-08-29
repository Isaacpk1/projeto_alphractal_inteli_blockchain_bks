using System.Data.Common;
using System.Globalization;

namespace Alphractal.Fees.Api.Repositories;

/// <summary>
/// Leitura por nome de coluna com conversao explicita.
/// </summary>
/// <remarks>
/// O driver do ClickHouse devolve <c>UInt64</c> como <see cref="ulong"/>,
/// <c>UInt32</c> como <see cref="uint"/> e <c>dateDiff</c> como <see cref="long"/>.
/// Um cast direto ((ulong)reader[0]) estoura em runtime se o tipo mudar na view;
/// <see cref="Convert"/> nao. Como a view e a fonte do formato, e barato ser
/// tolerante aqui e caro descobrir o problema em cima da demo.
/// </remarks>
internal static class DbReaderExtensions
{
    private static object? Raw(this DbDataReader reader, string column)
    {
        var ordinal = reader.GetOrdinal(column);
        return reader.IsDBNull(ordinal) ? null : reader.GetValue(ordinal);
    }

    public static ulong AsUInt64(this DbDataReader reader, string column)
        => Convert.ToUInt64(reader.Raw(column) ?? 0UL, CultureInfo.InvariantCulture);

    public static uint AsUInt32(this DbDataReader reader, string column)
        => Convert.ToUInt32(reader.Raw(column) ?? 0U, CultureInfo.InvariantCulture);

    public static long AsInt64(this DbDataReader reader, string column)
        => Convert.ToInt64(reader.Raw(column) ?? 0L, CultureInfo.InvariantCulture);

    public static double AsDouble(this DbDataReader reader, string column)
        => Convert.ToDouble(reader.Raw(column) ?? 0d, CultureInfo.InvariantCulture);

    public static decimal AsDecimal(this DbDataReader reader, string column)
        => Convert.ToDecimal(reader.Raw(column) ?? 0m, CultureInfo.InvariantCulture);

    public static string AsString(this DbDataReader reader, string column)
        => Convert.ToString(reader.Raw(column), CultureInfo.InvariantCulture) ?? string.Empty;

    /// <summary>
    /// Timestamps do ClickHouse sao UTC por definicao de coluna
    /// (<c>DateTime64(3, 'UTC')</c>), mas chegam com <see cref="DateTimeKind"/>
    /// indefinido. Marcar como UTC aqui evita a conversao silenciosa pelo fuso da
    /// maquina — que na maquina de quem apresenta seria UTC-3.
    /// </summary>
    public static DateTimeOffset AsUtc(this DbDataReader reader, string column)
    {
        var value = reader.Raw(column);
        return value switch
        {
            null => default,
            DateTimeOffset offset => offset.ToUniversalTime(),
            DateTime dateTime => new DateTimeOffset(DateTime.SpecifyKind(dateTime, DateTimeKind.Utc)),
            _ => new DateTimeOffset(DateTime.SpecifyKind(
                Convert.ToDateTime(value, CultureInfo.InvariantCulture), DateTimeKind.Utc)),
        };
    }

    public static DateOnly AsDate(this DbDataReader reader, string column)
        => DateOnly.FromDateTime(reader.AsUtc(column).UtcDateTime);
}
