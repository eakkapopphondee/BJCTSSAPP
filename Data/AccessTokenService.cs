using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.Data.SqlClient;

namespace thaibev.Data;

public sealed class AccessTokenService(ISqlDataAccess dataAccess, SqlObjectNames sqlObjects)
{
    public async Task<string> CreateAsync(string ipAddress, string? agent, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(ipAddress) || ipAddress.Length > 20)
            throw new ValidationException("Request IP address exceeds the 20-character database field.");
        if (agent?.Length > 500)
            throw new ValidationException("User-Agent exceeds the 500-character database field.");

        var table = $"{sqlObjects.Prefix}.[tsssAccessToken]";
        var sql = $"""
            SET XACT_ABORT ON; SET NOCOUNT ON;
            BEGIN TRY
                BEGIN TRANSACTION;
                IF EXISTS (SELECT 1 FROM {table} WITH (TABLOCKX, HOLDLOCK) WHERE [Code]=@Code)
                    THROW 51001, 'Generated access code already exists', 1;
                DECLARE @Saved TABLE ([Code] nvarchar(200));
                INSERT INTO {table}
                    ([Code], [IPAddress], [Agent], [CountUse], [DataStatus], [CreateUserID], [CreateUserDate])
                OUTPUT INSERTED.[Code] INTO @Saved
                VALUES (@Code, @IPAddress, @Agent, 0, 'A', 0, @CreateUserDate);
                COMMIT TRANSACTION;
                SELECT [Code] FROM @Saved;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH;
            """;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var code = Guid.NewGuid().ToString("D");
            try
            {
                var saved = await dataAccess.SelectAsync(sql,
                [
                    new("@Code", SqlDbType.NVarChar, 200) { Value = code },
                    new("@IPAddress", SqlDbType.VarChar, 20) { Value = ipAddress },
                    new("@Agent", SqlDbType.VarChar, 500) { Value = (object?)agent ?? DBNull.Value },
                    new("@CreateUserDate", SqlDbType.DateTime) { Value = DateTime.Now }
                ], cancellationToken);
                return saved.Rows[0].Field<string>("Code")!;
            }
            catch (SqlException error) when (attempt < 2 && error.Number is 51001 or 2601 or 2627)
            {
                // A new UUID plus a database check handles even a collision with an existing code.
            }
        }
        throw new InvalidOperationException("Unable to create a unique access code.");
    }
}
