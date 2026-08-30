using System.Data.Common;
using ClickHouse.Client.ADO;
using Microsoft.Extensions.Options;
using Alphractal.Fees.Api.Configuration;

namespace Alphractal.Fees.Api.Repositories;

public interface IClickHouseConnectionFactory
{
    /// <summary>Abre uma conexao ja apontada para o database configurado.</summary>
    Task<DbConnection> OpenAsync(CancellationToken cancellationToken);

    /// <summary>Timeout de comando, em segundos.</summary>
    int CommandTimeoutSeconds { get; }
}

/// <summary>
/// Monta a connection string do ClickHouse a partir de <see cref="ClickHouseOptions"/>.
/// </summary>
/// <remarks>
/// A API conecta como <c>alphractal_api</c>, que so tem GRANT de SELECT nas views
/// <c>v_*</c> (ver <c>infra/clickhouse/initdb/005_users.sql</c>). Se uma consulta
/// tocar tabela base, o proprio banco recusa — a separacao nao depende de
/// disciplina de quem escreve o SQL.
/// </remarks>
public sealed class ClickHouseConnectionFactory : IClickHouseConnectionFactory
{
    private readonly string _connectionString;

    public ClickHouseConnectionFactory(IOptions<ClickHouseOptions> options)
    {
        var value = options.Value;

        if (!Uri.TryCreate(value.BaseUrl, UriKind.Absolute, out var uri))
        {
            throw new InvalidOperationException(
                $"ClickHouse:BaseUrl invalida: '{value.BaseUrl}'. Esperado algo como http://localhost:8123");
        }

        CommandTimeoutSeconds = value.TimeoutSeconds;

        var builder = new ClickHouseConnectionStringBuilder
        {
            Host = uri.Host,
            Port = (ushort)(uri.IsDefaultPort ? 8123 : uri.Port),
            Database = value.Database,
            Username = value.User,
            Password = value.Password,
            Protocol = uri.Scheme,
            Timeout = TimeSpan.FromSeconds(value.TimeoutSeconds),
        };

        _connectionString = builder.ToString();
    }

    public int CommandTimeoutSeconds { get; }

    public async Task<DbConnection> OpenAsync(CancellationToken cancellationToken)
    {
        var connection = new ClickHouseConnection(_connectionString);
        try
        {
            await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
            return connection;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            await connection.DisposeAsync().ConfigureAwait(false);
            throw new ColdPathUnavailableException("Nao foi possivel abrir conexao com o ClickHouse.", exception);
        }
    }
}
