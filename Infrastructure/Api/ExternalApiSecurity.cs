using System.ComponentModel.DataAnnotations;
using System.Data;
using Microsoft.Data.SqlClient;
using thaibev.Data;

namespace thaibev.Infrastructure.Api;

public sealed class ExternalApiSecurity(ISqlDataAccess dataAccess, SqlObjectNames sqlObjects)
{
    public async Task ValidateTokenAsync(HttpContext context)
    {
        var accessToken = RequiredHeader(context, "AccessToken");
        var token = RequiredHeader(context, "Token");
        if (accessToken.Length > 200 || token.Length > 200)
            throw new ApiException(401, "AccessToken, Token Invaild");

        var table = $"{sqlObjects.Prefix}.[tsssToken]";
        var rows = await dataAccess.SelectAsync($"""
            SELECT TOP (1) [ExpiryTime] FROM {table}
            WHERE [Code]=@AccessToken AND [AccessTokenCode]=@Token
              AND [DataStatus]='A';
            """,
            [
                new("@AccessToken", SqlDbType.VarChar, 200) { Value = accessToken },
                new("@Token", SqlDbType.VarChar, 200) { Value = token }
            ], context.RequestAborted);
        if (rows.Rows.Count == 0) throw new ApiException(401, "AccessToken, Token Invaild");
        if (rows.Rows[0].Field<DateTime>("ExpiryTime") < DateTime.Now)
            throw new ApiException(401, "Token expired");
    }

    private static string RequiredHeader(HttpContext context, string name)
    {
        var values = context.Request.Headers[name];
        if (values.Count != 1 || string.IsNullOrWhiteSpace(values[0]))
            throw new ValidationException($"{name} header is required.");
        return values[0]!;
    }
}
