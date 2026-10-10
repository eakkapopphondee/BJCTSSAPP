using System.Data;
using System.Globalization;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Data.SqlClient;

namespace thaibev.Data;

public sealed record MenuPermission(int MenuID, string AccessFlag, string ExportFlag);

public sealed class UserAuthentication(ISqlDataAccess dataAccess, SqlObjectNames sqlObjects)
{
    public const string RoleIdClaim = "tsss:role_id";
    public const string RoleDescriptionClaim = "tsss:role_description";
    public const string MenuPermissionClaim = "tsss:menu_permission";

    public async Task<ClaimsPrincipal?> AuthenticateHashAsync(
        string username, string passwordHash, CancellationToken cancellationToken = default)
    {
        var sql = $"""
            SELECT u.[ID], u.[UserName], u.[UserFullName], u.[RoleID],
                   r.[RoleCode], r.[RoleDescription], p.[MenuID], p.[AccessFlag], p.[ExportFlag]
            FROM {sqlObjects.Prefix}.[tsssUser] u
            INNER JOIN {sqlObjects.Prefix}.[tsssRole] r
                ON r.[ID] = u.[RoleID] AND r.[DataStatus] = 'A'
            LEFT JOIN {sqlObjects.Prefix}.[tsssPermission] p
                ON p.[RoleID] = u.[RoleID] AND p.[DataStatus] = 'A'
            WHERE u.[UserName] = @UserName AND u.[Password] = @Password
                AND u.[DataStatus] = 'A';
            """;
        var table = await dataAccess.SelectAsync(sql,
            [new SqlParameter("@UserName", SqlDbType.VarChar, 50) { Value = username },
             new SqlParameter("@Password", SqlDbType.VarChar, 100) { Value = passwordHash }], cancellationToken);
        if (table.Rows.Count == 0) return null;
        var row = table.Rows[0];
        // Do not choose an arbitrary account if usernames are duplicated.
        if (table.Rows.Cast<DataRow>().Any(item => item.Field<int>("ID") != row.Field<int>("ID")))
            return null;
        List<Claim> claims =
        [
            new(ClaimTypes.NameIdentifier, row.Field<int>("ID").ToString(CultureInfo.InvariantCulture)),
            new(ClaimTypes.Name, row.Field<string>("UserFullName")!),
            new(ClaimTypes.Role, row.Field<string>("RoleCode")!),
            new(RoleIdClaim, row.Field<int>("RoleID").ToString(CultureInfo.InvariantCulture)),
            new(RoleDescriptionClaim, row.Field<string>("RoleDescription")!)
        ];
        foreach (var permission in table.Rows.Cast<DataRow>()
            .Where(item => !item.IsNull("MenuID"))
            .Select(item => new MenuPermission(item.Field<int>("MenuID"),
                item.Field<string>("AccessFlag")!, item.Field<string>("ExportFlag")!)).Distinct())
        {
            claims.Add(new Claim(MenuPermissionClaim, JsonSerializer.Serialize(permission)));
        }
        return new ClaimsPrincipal(new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme));
    }
}
