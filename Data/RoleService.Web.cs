using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using thaibev.Models;

namespace thaibev.Data;

public sealed partial class RoleService
{
    private string TableName => $"{sqlObjects.Prefix}.[tsssRole]";
    private const string SelectColumns = """
        [ID], [RoleCode], [RoleDescription],   
          [DataStatus], [CreateUserID], [CreateUserDate],
        [UpdateUserID], [UpdateUserDate]
        """;


    public Task<RoleViewModel> CreateModelAsync(CancellationToken cancellationToken) => Task.FromResult(new RoleViewModel());

    public async Task<RoleViewModel?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync(
            $"SELECT {SelectColumns} FROM {TableName} WHERE [ID] = @Id;",
            [new SqlParameter("@Id", SqlDbType.Int) { Value = id }], cancellationToken);
        return table.Rows.Count == 0 ? null : ToModel(table.Rows[0]);
    }



    private static RoleViewModel ToModel(DataRow row) => new()
    {
        Id = row.Field<int>("ID"),
        RoleCode = row.Field<string>("RoleCode") ?? string.Empty,
        RoleDescription = row.Field<string>("RoleDescription") ?? string.Empty,
        DataStatus = row.Field<string>("DataStatus") ?? string.Empty,
        CreateUserID = row.Field<int>("CreateUserID"),
        CreateUserDate = row.Field<DateTime>("CreateUserDate"),
        UpdateUserID = row.Field<int?>("UpdateUserID"),
        UpdateUserDate = row.Field<DateTime?>("UpdateUserDate")
    };

}
