namespace thaibev.Data;

/// <summary>Builds quoted SQL identifiers from trusted application configuration.</summary>
public sealed class SqlObjectNames
{
    public string Prefix { get; }

    public SqlObjectNames(IConfiguration configuration)
    {
        var database = configuration["Database:Name"];
        var schema = configuration["Database:Schema"];
        // An empty database uses the database selected by the connection string.
        Prefix = string.IsNullOrWhiteSpace(database)
            ? Quote(schema, "Database:Schema")
            : $"{Quote(database, "Database:Name")}.{Quote(schema, "Database:Schema")}";
    }

    private static string Quote(string? value, string key)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length > 128 || value.Contains('\0'))
            throw new InvalidOperationException($"{key} must contain a valid SQL identifier (1–128 characters).");
        return "[" + value.Replace("]", "]]") + "]";
    }
}
