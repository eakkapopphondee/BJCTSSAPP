using Microsoft.Data.SqlClient;

namespace thaibev.Data;

public interface ISqlConnectionFactory
{
    SqlConnection CreateConnection();
}

public sealed class SqlConnectionFactory(IConfiguration configuration) : ISqlConnectionFactory
{
    private readonly string _connectionString = configuration.GetConnectionString("DefaultConnection")
        ?? throw new InvalidOperationException(
            "Connection string 'DefaultConnection' was not found.");

    public SqlConnection CreateConnection()
    {
        return new SqlConnection(_connectionString);
    }
}
