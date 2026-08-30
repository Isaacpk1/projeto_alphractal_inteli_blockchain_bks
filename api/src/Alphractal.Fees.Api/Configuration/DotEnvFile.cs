namespace Alphractal.Fees.Api.Configuration;

/// <summary>
/// Carrega um arquivo <c>.env</c> para variaveis de ambiente do processo.
/// </summary>
/// <remarks>
/// O .NET nao le <c>.env</c> nativamente, mas le variaveis de ambiente. Entao o
/// caminho mais curto e transformar uma coisa na outra ANTES de construir o
/// host: nada de provider de configuracao customizado, e o mesmo arquivo serve
/// para <c>docker compose</c> depois.
/// <para>
/// Escolha deliberada de nao usar pacote: sao 40 linhas, e cada dependencia nova
/// e uma arvore transitiva a mais para auditar — o <c>Newtonsoft.Json</c> que a
/// Nethereum arrastou com CVE mostrou o custo disso na pratica.
/// </para>
/// <para>
/// Variavel ja definida no ambiente NAO e sobrescrita. Assim o <c>.env</c> e o
/// padrao do desenvolvedor, e o ambiente real (container, CI, user-secrets do
/// sistema) sempre vence — que e a ordem correta de precedencia.
/// </para>
/// </remarks>
public static class DotEnvFile
{
    /// <summary>
    /// Procura um <c>.env</c> a partir do diretorio atual e do diretorio do
    /// binario, subindo ate 5 niveis, e carrega o primeiro que encontrar.
    /// </summary>
    /// <remarks>
    /// A busca sobe niveis porque o diretorio de trabalho muda conforme como a
    /// aplicacao foi iniciada: <c>dotnet run --project ...</c> a partir de
    /// <c>api/</c>, <c>dotnet bin/.../App.dll</c> de dentro do projeto, ou o
    /// binario publicado. Fixar um caminho relativo funcionaria em um desses
    /// casos e falharia calado nos outros.
    /// </remarks>
    /// <returns>Caminho do arquivo carregado, ou <c>null</c> se nenhum existir.</returns>
    public static string? Load(string fileName = ".env")
    {
        var path = Find(fileName);
        if (path is null)
        {
            return null;
        }

        foreach (var rawLine in File.ReadLines(path))
        {
            var line = rawLine.Trim();

            if (line.Length == 0 || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith("export ", StringComparison.Ordinal))
            {
                line = line["export ".Length..].TrimStart();
            }

            var separator = line.IndexOf('=', StringComparison.Ordinal);
            if (separator <= 0)
            {
                continue;
            }

            var key = line[..separator].Trim();
            var value = line[(separator + 1)..].Trim();

            // Aspas sao delimitador, nao conteudo — uma URL entre aspas nao pode
            // virar parte do valor.
            if (value.Length >= 2
                && ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
            {
                value = value[1..^1];
            }

            if (key.Length == 0 || Environment.GetEnvironmentVariable(key) is not null)
            {
                continue;
            }

            Environment.SetEnvironmentVariable(key, value);
        }

        return path;
    }

    private static string? Find(string fileName)
    {
        var roots = new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory };

        foreach (var root in roots)
        {
            var directory = new DirectoryInfo(root);

            for (var level = 0; level < 5 && directory is not null; level++)
            {
                var candidate = Path.Combine(directory.FullName, fileName);
                if (File.Exists(candidate))
                {
                    return candidate;
                }

                directory = directory.Parent;
            }
        }

        return null;
    }
}
