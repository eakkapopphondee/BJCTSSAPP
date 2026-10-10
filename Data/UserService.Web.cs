using System.Data;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using thaibev.Infrastructure.Api;
using thaibev.Infrastructure.Validation;
using thaibev.Models;

namespace thaibev.Data;

public sealed partial class UserService
{
    private string TableName => Entity.Table(sqlObjects.Prefix);
    private string RoleTable => $"{sqlObjects.Prefix}.[tsssRole]";

    public async Task<UserViewModel> CreateModelAsync(CancellationToken cancellationToken)
    {
        var model = new UserViewModel();
        await LoadRoles(model, cancellationToken);
        return model;
    }

    public async Task<UserViewModel?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync($"SELECT {Entity.ColumnList}, [RoleDescription] {Entity.SearchFrom(sqlObjects.Prefix)} WHERE [ID]=@ID;",
            [new SqlParameter("@ID", id)], cancellationToken);
        if (table.Rows.Count == 0) return null;
        var model = ToModel(table.Rows[0]);
        await LoadRoles(model, cancellationToken);
        return model;
    }

    public async Task<Dictionary<string, object?>> ChangePasswordAsync(ChangeUserPasswordViewModel model, ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        InputValidation.ValidateModel(model);
        var output = string.Join(", ", Entity.PublicColumns.Select(column => $"INSERTED.[{column.Name}]"));
        var table = await dataAccess.SelectAsync($"""
            UPDATE {TableName} SET [Password]=@Password, [UpdateUserID]=@UpdateUserID, [UpdateUserDate]=@UpdateUserDate
            OUTPUT {output} WHERE [ID]=@ID;
            """, [new("@ID", model.Id), new("@Password", SqlDbType.VarChar, 100) { Value = passwordHash.Compute(model.Password) },
                new("@UpdateUserID", AuditStamp.UserId(user)), new("@UpdateUserDate", DateTime.Now)], cancellationToken);
        if (table.Rows.Count == 0) throw new ApiException(404, "Record ID was not found.");
        return SqlValues.ToRecord(table.Rows[0]);
    }

    private async Task LoadRoles(UserViewModel model, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync($"SELECT [ID], [RoleDescription] FROM {RoleTable} WHERE [DataStatus] = 'A' ORDER BY [RoleDescription], [ID];", [], cancellationToken);
        model.Roles = table.Rows.Cast<DataRow>().Select(row => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(row.Field<string>("RoleDescription"), row.Field<int>("ID").ToString())).ToList();
    }

    private static UserViewModel ToModel(DataRow row) => new()
    {
        Id = row.Field<int>("ID"), UserName = row.Field<string>("UserName")!, UserFullName = row.Field<string>("UserFullName")!,
        Email = row.Field<string>("email"), RoleID = row.Field<int>("RoleID"), RoleDescription = row.Field<string>("RoleDescription"),
        DataStatus = row.Field<string>("DataStatus")!, CreateUserID = row.Field<int>("CreateUserID"), CreateUserDate = row.Field<DateTime>("CreateUserDate"),
        UpdateUserID = row.Field<int?>("UpdateUserID"), UpdateUserDate = row.Field<DateTime?>("UpdateUserDate")
    };
}
