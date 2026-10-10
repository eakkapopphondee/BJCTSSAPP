using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using thaibev.Models;

namespace thaibev.Data;

public sealed partial class TruckTypeService
{
    private string TableName => $"{sqlObjects.Prefix}.[tsssTruckType]";
    private const string SelectColumns = """
        [ID], [TruckTypeCode], [TruckTypeName], [DefaultFlag],   
          [DataStatus], [CreateUserID], [CreateUserDate],
        [UpdateUserID], [UpdateUserDate]
        """;


    public Task<TruckTypeViewModel> CreateModelAsync(CancellationToken cancellationToken) => Task.FromResult(new TruckTypeViewModel());

    public async Task<TruckTypeViewModel?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync(
            $"SELECT {SelectColumns} FROM {TableName} WHERE [ID] = @Id;",
            [new SqlParameter("@Id", SqlDbType.Int) { Value = id }], cancellationToken);
        return table.Rows.Count == 0 ? null : ToModel(table.Rows[0]);
    }



    private static TruckTypeViewModel ToModel(DataRow row) => new()
    {
        Id = row.Field<int>("ID"),
        TruckTypeCode = row.Field<string>("TruckTypeCode") ?? string.Empty,
        TruckTypeName = row.Field<string>("TruckTypeName") ?? string.Empty,
        DefaultFlag = row.Field<string>("DefaultFlag") ?? "N",
        DataStatus = row.Field<string>("DataStatus") ?? string.Empty,
        CreateUserID = row.Field<int>("CreateUserID"),
        CreateUserDate = row.Field<DateTime>("CreateUserDate"),
        UpdateUserID = row.Field<int?>("UpdateUserID"),
        UpdateUserDate = row.Field<DateTime?>("UpdateUserDate")
    };

}
