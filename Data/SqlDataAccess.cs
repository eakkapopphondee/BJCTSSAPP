using System.Data;
using Microsoft.Data.SqlClient;

namespace thaibev.Data;

public interface ISqlDataAccess
{
    Task<int> InsertAsync(
        string sql,
        SqlParameter[]? parameters = null,
        CancellationToken cancellationToken = default);

    Task<int> UpdateAsync(
        string sql,
        SqlParameter[]? parameters = null,
        CancellationToken cancellationToken = default);

    Task<int> DeleteAsync(
        string sql,
        SqlParameter[]? parameters = null,
        CancellationToken cancellationToken = default);

    Task<DataTable> SelectAsync(
        string sql,
        SqlParameter[]? parameters = null,
        CancellationToken cancellationToken = default);
}

public sealed class SqlDataAccess(ISqlConnectionFactory connectionFactory) : ISqlDataAccess
{
    public Task<int> InsertAsync(
        string sql,
        SqlParameter[]? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteNonQueryAsync(sql, parameters, cancellationToken);
    }

    public Task<int> UpdateAsync(
        string sql,
        SqlParameter[]? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteNonQueryAsync(sql, parameters, cancellationToken);
    }

    public Task<int> DeleteAsync(
        string sql,
        SqlParameter[]? parameters = null,
        CancellationToken cancellationToken = default)
    {
        return ExecuteNonQueryAsync(sql, parameters, cancellationToken);
    }

    public async Task<DataTable> SelectAsync(
        string sql,
        SqlParameter[]? parameters = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = CreateCommand(connection, sql, parameters);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        var result = new DataTable();
        result.Load(reader);
        return result;
    }

    private async Task<int> ExecuteNonQueryAsync(
        string sql,
        SqlParameter[]? parameters,
        CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(sql);

        await using var connection = connectionFactory.CreateConnection();
        await connection.OpenAsync(cancellationToken);

        await using var command = CreateCommand(connection, sql, parameters);
        return await command.ExecuteNonQueryAsync(cancellationToken);
    }

    private static SqlCommand CreateCommand(
        SqlConnection connection,
        string sql,
        SqlParameter[]? parameters)
    {
        var command = new SqlCommand(sql, connection)
        {
            CommandType = CommandType.Text
        };

        if (parameters is { Length: > 0 })
        {
            command.Parameters.AddRange(parameters);
        }

        return command;
    }
}
