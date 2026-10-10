using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using thaibev.Infrastructure.Api;
using thaibev.Infrastructure.Validation;
using thaibev.Models;

namespace thaibev.Data;

public sealed class PermissionService(ISqlDataAccess dataAccess, SqlObjectNames sqlObjects)
{
    public async Task<List<Dictionary<string, object?>>> ListAsync(PermissionListRequest request, CancellationToken cancellationToken)
    {
        InputValidation.ValidateModel(request);
        var table = await dataAccess.SelectAsync($"""
            SELECT menu.[ID] AS [MenuID], @RoleID AS [RoleID], menu.[MenuName], menu.[SubjectFlag], menu.[MenuItemNo],
                   COALESCE(permission.[AccessFlag], 'N') AS [AccessFlag],
                   CASE WHEN permission.[AccessFlag]='Y' THEN COALESCE(permission.[ExportFlag], 'N') ELSE 'N' END AS [ExportFlag]
            FROM {sqlObjects.Prefix}.[tsssMenu] menu
            LEFT JOIN {sqlObjects.Prefix}.[tsssPermission] permission
                ON permission.[MenuID]=menu.[ID] AND permission.[RoleID]=@RoleID AND permission.[DataStatus]='A'
            WHERE menu.[DataStatus]='A'
            ORDER BY menu.[MenuItemNo], menu.[ID];
            """, [new("@RoleID", SqlDbType.Int) { Value = request.RoleID }], cancellationToken);
        return table.Rows.Cast<DataRow>().Select(SqlValues.ToRecord).ToList();
    }

    public async Task<ApiResponse> SearchAsync(PermissionSearchRequest request, CancellationToken cancellationToken)
    {
        var rows = await ListAsync(request, cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(rows.Count / (double)request.PerPage));
        var page = Math.Min(request.Page, totalPages);
        return ApiResponse.Success(rows.Skip((page - 1) * request.PerPage).Take(request.PerPage).ToArray(),
            new Dictionary<string, object?> { ["Pagination"] = new { Page = page, request.PerPage, TotalCount = rows.Count, TotalPages = totalPages } });
    }

    public async Task<object> SaveAsync(PermissionSaveRequest request, ClaimsPrincipal? user, CancellationToken cancellationToken)
    {
        InputValidation.ValidateModel(request);
        foreach (var item in request.Permissions!)
        {
            if (item is null) throw new ValidationException("Permission item is required.");
            InputValidation.ValidateModel(item);
        }
        if (request.Permissions.Select(item => item.MenuID).Distinct().Count() != request.Permissions.Count)
            throw new ValidationException("Duplicate MenuID is not allowed.");
        var userId = user is null ? request.UpdateUserID : AuditStamp.UserId(user);
        if (userId is null or <= 0) throw new ValidationException("UpdateUserID must be a positive integer.");
        var date = user is null ? request.UpdateUserDate ?? DateTime.Now : DateTime.Now;
        InputValidation.ValidateSqlDateTime(date, "UpdateUserDate");
        var payload = JsonSerializer.Serialize(request.Permissions.Select(item => new
        {
            item.MenuID, item.AccessFlag, ExportFlag = item.AccessFlag == "Y" ? item.ExportFlag : "N"
        }));
        try
        {
            await dataAccess.SelectAsync($"""
                SET XACT_ABORT ON; SET NOCOUNT ON;
                BEGIN TRY
                    BEGIN TRANSACTION;
                    IF NOT EXISTS (SELECT 1 FROM {sqlObjects.Prefix}.[tsssRole] WITH (UPDLOCK, HOLDLOCK) WHERE [ID]=@RoleID)
                        THROW 51004, 'RoleID was not found', 1;
                    SELECT [MenuID], [AccessFlag], [ExportFlag] INTO #PermissionInput
                    FROM OPENJSON(@Permissions) WITH ([MenuID] int, [AccessFlag] char(1), [ExportFlag] char(1));
                    IF EXISTS (SELECT 1 FROM #PermissionInput input WHERE NOT EXISTS
                        (SELECT 1 FROM {sqlObjects.Prefix}.[tsssMenu] WITH (UPDLOCK, HOLDLOCK) WHERE [ID]=input.[MenuID] AND [DataStatus]='A'))
                        THROW 51005, 'MenuID must reference an active menu', 1;
                    UPDATE permission SET [AccessFlag]=input.[AccessFlag], [ExportFlag]=input.[ExportFlag],
                        [DataStatus]='A', [UpdateUserID]=@UserID, [UpdateUserDate]=@Date
                    FROM {sqlObjects.Prefix}.[tsssPermission] permission WITH (UPDLOCK, HOLDLOCK)
                    INNER JOIN #PermissionInput input ON input.[MenuID]=permission.[MenuID]
                    WHERE permission.[RoleID]=@RoleID;
                    INSERT INTO {sqlObjects.Prefix}.[tsssPermission]
                        ([RoleID], [MenuID], [AccessFlag], [ExportFlag], [DataStatus], [CreateUserID], [CreateUserDate], [UpdateUserID], [UpdateUserDate])
                    SELECT @RoleID, input.[MenuID], input.[AccessFlag], input.[ExportFlag], 'A', @UserID, @Date, @UserID, @Date
                    FROM #PermissionInput input WHERE NOT EXISTS
                        (SELECT 1 FROM {sqlObjects.Prefix}.[tsssPermission] WITH (UPDLOCK, HOLDLOCK)
                         WHERE [RoleID]=@RoleID AND [MenuID]=input.[MenuID]);
                    DROP TABLE #PermissionInput;
                    COMMIT TRANSACTION;
                    SELECT @RoleID AS [RoleID];
                END TRY
                BEGIN CATCH
                    IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                    THROW;
                END CATCH;
                """, [new("@RoleID", SqlDbType.Int) { Value = request.RoleID },
                    new("@Permissions", SqlDbType.NVarChar, -1) { Value = payload },
                    new("@UserID", SqlDbType.Int) { Value = userId }, new("@Date", SqlDbType.DateTime) { Value = date }], cancellationToken);
        }
        catch (SqlException error) when (error.Number is 51004 or 51005)
        {
            throw new ApiException(error.Number == 51004 ? 404 : 400, error.Message, error);
        }
        return new { request.RoleID };
    }
}
