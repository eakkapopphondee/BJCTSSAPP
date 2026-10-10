using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using thaibev.Infrastructure.Api;
using thaibev.Infrastructure.Validation;
using thaibev.Models;

namespace thaibev.Data;

// User owns its queries, validation and mutations; the method layout matches the other table services.
public sealed partial class UserService(ISqlDataAccess dataAccess, SqlObjectNames sqlObjects, PasswordHash passwordHash)
{
    private static readonly JsonSerializerOptions ModelJson = new() { PropertyNameCaseInsensitive = true };
    private static readonly EntityDefinition Entity = new("User", typeof(UserViewModel),
        [
            new("ID", SqlDbType.Int),
            new("UserName", SqlDbType.VarChar, 50),
            new("UserFullName", SqlDbType.NVarChar, 150),
            new("email", SqlDbType.VarChar, 80, true),
            new("RoleID", SqlDbType.Int),
            new("Password", SqlDbType.VarChar, 100),
            new("DataStatus", SqlDbType.Char, 1),
            new("CreateUserID", SqlDbType.Int),
            new("CreateUserDate", SqlDbType.DateTime),
            new("UpdateUserID", SqlDbType.Int, 0, true),
            new("UpdateUserDate", SqlDbType.DateTime, 0, true)
        ], """
        WHERE (@Search = N'' OR CONVERT(nvarchar(20), [ID]) LIKE @LikeSearch OR [UserFullName] LIKE @LikeSearch)
                AND (@DataStatus = 'L' OR [DataStatus] = @DataStatus)
        """, ["UserName"], ["RoleDescription"]);

    public async Task<UserPage> SearchAsync(UserSearchRequest request, CancellationToken cancellationToken)
    {
        InputValidation.ValidateModel(request);
        var entity = Entity;
        var count = await dataAccess.SelectAsync($"SELECT COUNT(*) {entity.SearchFrom(sqlObjects.Prefix)} {entity.SearchWhere};",
            Filters(entity, request), cancellationToken);
        var totalCount = Convert.ToInt32(count.Rows[0][0]);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)request.PerPage));
        var page = Math.Min(request.Page, totalPages);
        var rows = await SelectRows(entity, request,
            "OFFSET @Offset ROWS FETCH NEXT @PerPage ROWS ONLY",
            [new("@Offset", SqlDbType.Int) { Value = (page - 1) * request.PerPage }, new("@PerPage", SqlDbType.Int) { Value = request.PerPage }], cancellationToken);
        return new(rows, page, request.PerPage, totalCount, totalPages);
    }

    public async Task<List<Dictionary<string, object?>>> ListAsync(UserListRequest request, CancellationToken cancellationToken)
    {
        InputValidation.ValidateModel(request);
        var entity = Entity;
        return await SelectRows(entity, request, "", [], cancellationToken);
    }

    private async Task<List<Dictionary<string, object?>>> SelectRows(EntityDefinition entity, UserListRequest request,
        string pagination, SqlParameter[] paginationParameters, CancellationToken cancellationToken)
    {
        var extra = string.Concat(entity.LookupFields.Select(field => $", [{field}]"));
        var table = await dataAccess.SelectAsync($"""
            SELECT {entity.ColumnList}{extra} {entity.SearchFrom(sqlObjects.Prefix)} {entity.SearchWhere}
            ORDER BY [ID] DESC {pagination};
            """, [.. Filters(entity, request), .. paginationParameters], cancellationToken);
        return table.Rows.Cast<DataRow>().Select(SqlValues.ToRecord).ToList();
    }

    private static SqlParameter[] Filters(EntityDefinition entity, UserListRequest request)
    {
        var text = request.TextSearch?.Trim() ?? "";
        var filters = new List<SqlParameter>
        {
            new("@Search", SqlDbType.NVarChar, -1) { Value = text },
            new("@LikeSearch", SqlDbType.NVarChar, -1) { Value = $"%{text}%" },
            new("@DataStatus", SqlDbType.Char, 1) { Value = request.DataStatus }
        };
        return filters.ToArray();
    }

    public static ApiResponse Response(List<Dictionary<string, object?>> rows, object? pagination = null)
    {
        var entity = Entity;
        var metadata = new Dictionary<string, object?>();
        if (pagination is not null) metadata["Pagination"] = pagination;
        if (entity.LookupFields.Length > 0)
            metadata["Lookups"] = rows.Select(row => row.Where(pair => pair.Key == "ID" || entity.LookupFields.Contains(pair.Key)).ToDictionary());
        var records = rows.Select(row => entity.PublicColumns.ToDictionary(column => column.Name, column => row[column.Name]));
        return ApiResponse.Success(records.ToArray(), metadata.Count > 0 ? metadata : null);
    }

    public static ApiResponse Response(UserPage page) => Response(page.Rows,
        new { page.Page, page.PerPage, page.TotalCount, page.TotalPages });

    public async Task<Dictionary<string, object?>> SaveAsync(JsonElement body, ClaimsPrincipal? webUser, CancellationToken cancellationToken)
    {
        var entity = Entity;
        var input = InputValidation.ReadObject(body);
        var id = input.TryGetValue("ID", out var idValue) ? (int?)SqlValues.Read(new("ID", SqlDbType.Int), idValue) ?? 0 : 0;
        if (id < 0) throw new ValidationException("ID must be zero or a positive integer.");
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase) { ["ID"] = id };
        foreach (var column in entity.Columns.Where(column => column.Name is not ("ID" or "Password") && !AuditStamp.IsAuditField(column.Name)))
        {
            input.TryGetValue(column.Name, out var value);
            values[column.Name] = SqlValues.Read(column, value);
        }
        if (Convert.ToString(values["DataStatus"]) is not ("A" or "C")) throw new ValidationException("DataStatus must be A or C.");

        // Reuse the User model's username, full name, email, role and status validation.
        var model = JsonSerializer.Deserialize(JsonSerializer.Serialize(values), entity.ModelType, ModelJson)!;
        InputValidation.ValidateModel(model);
        // Password is plaintext input only. Omission on update preserves the stored hash.
        var hasPassword = input.TryGetValue("Password", out var passwordValue);
        if (hasPassword || id == 0)
        {
            if (passwordValue.ValueKind != JsonValueKind.String || string.IsNullOrWhiteSpace(passwordValue.GetString()))
                throw new ValidationException("Password is required and must be a non-empty string.");
            var password = passwordValue.GetString()!;
            if (webUser is not null && (!input.TryGetValue("ConfirmPassword", out var confirmation)
                || confirmation.ValueKind != JsonValueKind.String || confirmation.GetString() != password))
                throw new ValidationException("Password and ConfirmPassword must match.");
            values["Password"] = passwordHash.Compute(password);
        }
        else values["Password"] = null;
        var now = DateTime.Now;
        foreach (var column in entity.Columns.Where(column => AuditStamp.IsAuditField(column.Name)))
        {
            if (webUser is not null)
                values[column.Name] = column.Name switch
                {
                    "CreateUserID" => AuditStamp.UserId(webUser), "CreateUserDate" => now,
                    "UpdateUserID" => id > 0 ? AuditStamp.UserId(webUser) : null,
                    _ => id > 0 ? now : null
                };
            else if (id > 0 && column.Name.StartsWith("Create", StringComparison.Ordinal))
                values[column.Name] = column.Name == "CreateUserID" ? 0 : now; // Unused update parameters.
            else
            {
                input.TryGetValue(column.Name, out var value);
                values[column.Name] = SqlValues.Read(column, value);
            }
        }
        if (id == 0 && (int)values["CreateUserID"]! <= 0) throw new ValidationException("CreateUserID must be a positive integer.");
        var table = entity.Table(sqlObjects.Prefix);
        var writeColumns = entity.Columns.Where(column => column.Name != "ID").ToArray();
        var output = string.Join(", ", entity.PublicColumns.Select(column => $"INSERTED.[{column.Name}]"));
        var assignments = writeColumns.Where(column => !column.Name.StartsWith("Create", StringComparison.Ordinal)).Select(column =>
            column.Name == "Password" ? $"[{column.Name}] = COALESCE(@{column.Name}, [{column.Name}])" : $"[{column.Name}] = @{column.Name}");
        var duplicates = string.Join("\n", entity.DuplicateFields.Select(field => $"""
            IF EXISTS (SELECT 1 FROM {table} WITH (UPDLOCK, HOLDLOCK) WHERE [{field}]=@{field} AND [ID]<>@ID)
                THROW 51001, '{entity.Name}: duplicate {field}', 1;
            """));
        var sql = $"""
            SET XACT_ABORT ON; SET NOCOUNT ON;
            BEGIN TRY
                BEGIN TRANSACTION;
                SELECT TOP (0) {entity.ColumnList.Replace("[ID]", "[ID] + 0 AS [ID]")} INTO #SavedSetup FROM {table};
                IF @ID > 0 AND NOT EXISTS (SELECT 1 FROM {table} WITH (UPDLOCK, HOLDLOCK) WHERE [ID]=@ID)
                    THROW 51004, 'Record ID was not found', 1;
                {ReferenceGuards(entity)}
                {duplicates}

                IF @ID > 0
                    UPDATE {table} SET {string.Join(", ", assignments)} OUTPUT {output} INTO #SavedSetup WHERE [ID]=@ID;
                ELSE
                    INSERT INTO {table} ({string.Join(", ", writeColumns.Select(column => $"[{column.Name}]"))})
                    OUTPUT {output} INTO #SavedSetup VALUES ({string.Join(", ", writeColumns.Select(column => $"@{column.Name}"))});
                COMMIT TRANSACTION;
                SELECT {entity.ColumnList} FROM #SavedSetup;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH;
            """;
        return await Mutate(sql, entity.Columns.Select(column => SqlValues.Parameter(column, values[column.Name])).ToArray(), cancellationToken);
    }

    public async Task<Dictionary<string, object?>> DeleteAsync(int id, int? updateUserId, DateTime? updateDate,
        CancellationToken cancellationToken)
    {
        if (id <= 0) throw new ValidationException("ID must be a positive integer.");
        if (updateDate.HasValue) InputValidation.ValidateSqlDateTime(updateDate.Value, "UpdateUserDate");
        var entity = Entity;
        var table = entity.Table(sqlObjects.Prefix);
        var output = string.Join(", ", entity.PublicColumns.Select(column => $"INSERTED.[{column.Name}]"));
        var sql = $"""
            SET XACT_ABORT ON; SET NOCOUNT ON;
            BEGIN TRY
                BEGIN TRANSACTION;
                SELECT TOP (0) {entity.ColumnList.Replace("[ID]", "[ID] + 0 AS [ID]")} INTO #SavedSetup FROM {table};
                IF NOT EXISTS (SELECT 1 FROM {table} WITH (UPDLOCK, HOLDLOCK) WHERE [ID]=@ID)
                    THROW 51004, 'Record ID was not found', 1;

                UPDATE {table} SET [DataStatus]='C', [UpdateUserID]=@UpdateUserID, [UpdateUserDate]=@UpdateUserDate
                OUTPUT {output} INTO #SavedSetup WHERE [ID]=@ID;
                COMMIT TRANSACTION;
                SELECT {entity.ColumnList} FROM #SavedSetup;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH;
            """;
        return await Mutate(sql,
        [new("@ID", SqlDbType.Int) { Value = id }, new("@UpdateUserID", SqlDbType.Int) { Value = (object?)updateUserId ?? DBNull.Value },
         new("@UpdateUserDate", SqlDbType.DateTime) { Value = (object?)updateDate ?? DBNull.Value }], cancellationToken);
    }

    private async Task<Dictionary<string, object?>> Mutate(string sql, SqlParameter[] parameters, CancellationToken cancellationToken)
    {
        try { return SqlValues.ToRecord((await dataAccess.SelectAsync(sql, parameters, cancellationToken)).Rows[0]); }
        catch (SqlException error) when (error.Number is >= 51001 and <= 51006)
        {
            var status = error.Number switch { 51004 => 404, 51005 => 400, _ => 409 };
            throw new ApiException(status, error.Message, error);
        }
    }

    private string ReferenceGuards(EntityDefinition entity) => $"""
        IF NOT EXISTS (SELECT 1 FROM {sqlObjects.Prefix}.[tsssRole] WITH (UPDLOCK, HOLDLOCK) WHERE [ID]=@RoleID AND [DataStatus]='A')
            THROW 51005, 'An active RoleID is required', 1;
        """;

    // User owns its schema. Password hashes are excluded from every returned record.
    private sealed record EntityDefinition(string Name, Type ModelType, SqlColumn[] Columns, string SearchWhere, string[] DuplicateFields, string[] LookupFields)
    {
        public string Table(string prefix) => $"{prefix}.[tsssUser]";
        public IEnumerable<SqlColumn> PublicColumns => Columns.Where(column => column.Name != "Password");
        public string ColumnList => string.Join(", ", PublicColumns.Select(column => $"[{column.Name}]"));
        public string SearchFrom(string prefix) =>
            $"FROM (SELECT {string.Join(", ", PublicColumns.Select(column => $"u.[{column.Name}]"))}, r.[RoleDescription] FROM {Table(prefix)} u LEFT JOIN {prefix}.[tsssRole] r ON r.[ID]=u.[RoleID]) records";
    }
}
