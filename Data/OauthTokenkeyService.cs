using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Options;
using thaibev.Infrastructure.Api;
using thaibev.Models;

namespace thaibev.Data;

public sealed class OauthTokenkeyOptions
{
    public string VendorID { get; set; } = string.Empty;
    public string AuthKey { get; set; } = string.Empty;
    public int ExpiryMinutes { get; set; } = 60;
}

public sealed class OauthTokenkeyService(ISqlDataAccess dataAccess, SqlObjectNames sqlObjects,
    IOptionsMonitor<OauthTokenkeyOptions> options)
{
    public async Task<string> CreateAsync(string accessToken, OauthTokenkeyRequest request, CancellationToken cancellationToken)
    {
        if (accessToken.Length > 200)
            throw new ValidationException("AccessToken exceeds the 200-character database field.");

        var accessTable = $"{sqlObjects.Prefix}.[tsssAccessToken]";
        var active = await dataAccess.SelectAsync($"""
            SELECT TOP (1) [Code] FROM {accessTable}
            WHERE [Code]=@Code AND [DataStatus]='A';
            """, [new("@Code", SqlDbType.VarChar, 200) { Value = accessToken }], cancellationToken);
        if (active.Rows.Count == 0)
            throw new ApiException(403, "AccessToken has no permission to call this API.");

        var settings = options.CurrentValue;
        if (string.IsNullOrWhiteSpace(settings.VendorID) || string.IsNullOrWhiteSpace(settings.AuthKey)
            || settings.ExpiryMinutes <= 0)
            throw new InvalidOperationException("Oauth:Tokenkey must contain VendorID, AuthKey and a positive ExpiryMinutes.");
        if (!Matches(request.VendorID, settings.VendorID) || !Matches(request.AuthKey, settings.AuthKey))
            throw new ApiException(403, "Invalid vendor credentials.");

        var now = DateTime.Now;
        DateTime expiry;
        try { expiry = now.AddMinutes(settings.ExpiryMinutes); }
        catch (ArgumentOutOfRangeException error)
        {
            throw new InvalidOperationException("Oauth:Tokenkey:ExpiryMinutes is too large.", error);
        }
        if (expiry >= new DateTime(9999, 12, 31, 23, 59, 59, 998))
            throw new InvalidOperationException("Oauth:Tokenkey:ExpiryMinutes exceeds the SQL datetime range.");

        var tokenTable = $"{sqlObjects.Prefix}.[tsssToken]";
        var sql = $"""
            SET XACT_ABORT ON; SET NOCOUNT ON;
            BEGIN TRY
                BEGIN TRANSACTION;
                IF NOT EXISTS (SELECT 1 FROM {accessTable} WITH (UPDLOCK, HOLDLOCK)
                    WHERE [Code]=@Code AND [DataStatus]='A')
                    THROW 51002, 'AccessToken has no permission to call this API', 1;
                IF EXISTS (SELECT 1 FROM {tokenTable} WITH (TABLOCKX, HOLDLOCK) WHERE [AccessTokenCode]=@NewToken)
                    THROW 51001, 'Generated token already exists', 1;
                DECLARE @Saved TABLE ([AccessTokenCode] varchar(200));
                INSERT INTO {tokenTable}
                    ([Code], [UserID], [AccessTokenCode], [ExpiryTime], [Type], [DataStatus], [CreateUserID], [CreateUserDate])
                OUTPUT INSERTED.[AccessTokenCode] INTO @Saved
                VALUES (@Code, 0, @NewToken, @ExpiryTime, '', 'A', 0, @CreateUserDate);
                COMMIT TRANSACTION;
                SELECT [AccessTokenCode] FROM @Saved;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH;
            """;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var token = Guid.NewGuid().ToString("D");
            try
            {
                var saved = await dataAccess.SelectAsync(sql,
                [
                    new("@Code", SqlDbType.VarChar, 200) { Value = accessToken },
                    new("@NewToken", SqlDbType.VarChar, 200) { Value = token },
                    new("@ExpiryTime", SqlDbType.DateTime) { Value = expiry },
                    new("@CreateUserDate", SqlDbType.DateTime) { Value = now }
                ], cancellationToken);
                return saved.Rows[0].Field<string>("AccessTokenCode")!;
            }
            catch (SqlException error) when (error.Number == 51002)
            {
                throw new ApiException(403, "AccessToken has no permission to call this API.", error);
            }
            catch (SqlException error) when (attempt < 2 && error.Number is 51001 or 2601 or 2627)
            {
                // Retry if a generated UUID is already present.
            }
        }
        throw new InvalidOperationException("Unable to create a unique token.");
    }

    private static bool Matches(string candidate, string configured) =>
        CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(candidate), Encoding.UTF8.GetBytes(configured));
}
