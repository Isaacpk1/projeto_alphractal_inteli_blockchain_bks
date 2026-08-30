using Alphractal.Fees.Api.Configuration;
using Xunit;

namespace Alphractal.Fees.Tests;

/// <summary>
/// O parser do <c>.env</c>. Testado porque um erro aqui e silencioso: a variavel
/// simplesmente nao existe, a API sobe sem ingestao e o log so diz "nao
/// configurada" — sem apontar o arquivo mal interpretado.
/// </summary>
public sealed class DotEnvFileTests : IDisposable
{
    private readonly string _directory = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly List<string> _touched = [];

    public DotEnvFileTests() => Directory.CreateDirectory(_directory);

    [Fact]
    public void Le_pares_ignora_comentarios_e_remove_aspas()
    {
        var name = Unique("A");
        Write($"""
        # comentario
        {name}="wss://eth-mainnet.g.alchemy.com/v2/abc"

        export {name}_EXPORTADA=valor
        {name}_SIMPLES=sem-aspas
        linha-sem-igual
        """);

        Load();

        Assert.Equal("wss://eth-mainnet.g.alchemy.com/v2/abc", Environment.GetEnvironmentVariable(name));
        Assert.Equal("valor", Environment.GetEnvironmentVariable($"{name}_EXPORTADA"));
        Assert.Equal("sem-aspas", Environment.GetEnvironmentVariable($"{name}_SIMPLES"));
    }

    [Fact]
    public void Ambiente_existente_vence_o_arquivo()
    {
        // Precedencia que permite container e CI sobreporem sem editar arquivo.
        var name = Unique("B");
        Environment.SetEnvironmentVariable(name, "do-ambiente");
        _touched.Add(name);

        Write($"{name}=do-arquivo");
        Load();

        Assert.Equal("do-ambiente", Environment.GetEnvironmentVariable(name));
    }

    [Fact]
    public void Valor_com_igual_no_meio_nao_e_truncado()
    {
        // Uma URL com query string tem '=' — dividir em todos quebraria a chave.
        var name = Unique("C");
        Write($"{name}=https://host/path?id=ethereum&vs=usd");

        Load();

        Assert.Equal("https://host/path?id=ethereum&vs=usd", Environment.GetEnvironmentVariable(name));
    }

    [Fact]
    public void Arquivo_ausente_devolve_null_em_vez_de_lancar()
    {
        Assert.Null(DotEnvFile.Load(Path.GetRandomFileName() + ".env"));
    }

    private string Unique(string suffix)
    {
        var name = $"ALPHRACTAL_TEST_{suffix}_{Guid.NewGuid():N}";
        _touched.Add(name);
        return name;
    }

    private void Write(string content)
        => File.WriteAllText(Path.Combine(_directory, ".env.test"), content);

    private void Load()
    {
        var previous = Directory.GetCurrentDirectory();
        Directory.SetCurrentDirectory(_directory);
        try
        {
            DotEnvFile.Load(".env.test");
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
        }
    }

    public void Dispose()
    {
        foreach (var name in _touched)
        {
            Environment.SetEnvironmentVariable(name, null);
        }

        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }
}
