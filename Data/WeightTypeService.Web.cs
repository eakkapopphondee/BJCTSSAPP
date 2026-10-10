using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using thaibev.Models;

namespace thaibev.Data;

public sealed partial class WeightTypeService
{
    private string TableName => $"{sqlObjects.Prefix}.[tsssWeightType]";
    private const string SelectColumns = """
        [ID], [WeightTypeCode], [WeightTypeName],   
          [DataStatus], [CreateUserID], [CreateUserDate],
        [UpdateUserID], [UpdateUserDate]
        """;


    public Task<WeightTypeViewModel> CreateModelAsync(CancellationToken cancellationToken) => Task.FromResult(new WeightTypeViewModel());

    public async Task<WeightTypeViewModel?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync(
            $"SELECT {SelectColumns} FROM {TableName} WHERE [ID] = @Id;",
            [new SqlParameter("@Id", SqlDbType.Int) { Value = id }], cancellationToken);
        return table.Rows.Count == 0 ? null : ToModel(table.Rows[0]);
    }



    private static WeightTypeViewModel ToModel(DataRow row) => new()
    {
        Id = row.Field<int>("ID"),
        WeightTypeCode = row.Field<string>("WeightTypeCode") ?? string.Empty,
        WeightTypeName = row.Field<string>("WeightTypeName") ?? string.Empty,
        DataStatus = row.Field<string>("DataStatus") ?? string.Empty,
        CreateUserID = row.Field<int>("CreateUserID"),
        CreateUserDate = row.Field<DateTime>("CreateUserDate"),
        UpdateUserID = row.Field<int?>("UpdateUserID"),
        UpdateUserDate = row.Field<DateTime?>("UpdateUserDate")
    };

}
