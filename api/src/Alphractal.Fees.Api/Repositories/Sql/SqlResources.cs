using System.Collections.Concurrent;
using System.Reflection;

namespace Alphractal.Fees.Api.Repositories.Sql;

/// <summary>
/// Carrega os arquivos <c>.sql</c> desta pasta como recurso embutido.
/// SQL nao mora em string dentro do <c>.cs</c> — a regra e do README desta pasta.
/// </summary>
/// <remarks>
/// Os arquivos entram no assembly pelo glob de <c>EmbeddedResource</c> no
/// <c>.csproj</c>. O nome do recurso e
/// <c>Alphractal.Fees.Api.Repositories.Sql.&lt;arquivo&gt;.sql</c>.
/// Um nome errado falha no primeiro acesso, com a lista do que existe — nao
/// silenciosamente.
/// </remarks>
public static class SqlResources
{
    private const string Prefix = "Alphractal.Fees.Api.Repositories.Sql.";

    private static readonly Assembly OwnAssembly = typeof(SqlResources).Assembly;
    private static readonly ConcurrentDictionary<string, string> Cache = new(StringComparer.Ordinal);

    public static string Load(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        return Cache.GetOrAdd(name, static key =>
        {
            var resource = Prefix + key + ".sql";
            using var stream = OwnAssembly.GetManifestResourceStream(resource);
            if (stream is null)
            {
                var available = string.Join(", ", OwnAssembly
                    .GetManifestResourceNames()
                    .Where(item => item.StartsWith(Prefix, StringComparison.Ordinal))
                    .Order(StringComparer.Ordinal));
                throw new InvalidOperationException(
                    $"Consulta SQL nao embutida: '{resource}'. Disponiveis: {available}");
            }

            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        });
    }

    public static string LatestBlock => Load("latest_block");
    public static string MempoolNow => Load("mempool_now");
    public static string FeeEstimatesNow => Load("fee_estimates_now");
    public static string FeesHistoryHourly => Load("fees_history_hourly");
    public static string FeesHistoryDaily => Load("fees_history_daily");
    public static string FeeEstimatesDaily => Load("fee_estimates_daily");
    public static string IngestionStatus => Load("ingestion_status");
}
