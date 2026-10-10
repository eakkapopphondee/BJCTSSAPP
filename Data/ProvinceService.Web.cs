using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using thaibev.Models;

namespace thaibev.Data;

public sealed partial class ProvinceService
{
    private string TableName => $"{sqlObjects.Prefix}.[tsssProvince]";
    private const string SelectColumns = """
        [ID], [ProvinceCode], [ProvinceName],   
          [DataStatus], [CreateUserID], [CreateUserDate],
        [UpdateUserID], [UpdateUserDate]
        """;


    public Task<ProvinceViewModel> CreateModelAsync(CancellationToken cancellationToken) => Task.FromResult(new ProvinceViewModel());

    public async Task<ProvinceViewModel?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync(
            $"SELECT {SelectColumns} FROM {TableName} WHERE [ID] = @Id;",
            [new SqlParameter("@Id", SqlDbType.Int) { Value = id }], cancellationToken);
        return table.Rows.Count == 0 ? null : ToModel(table.Rows[0]);
    }



    private static ProvinceViewModel ToModel(DataRow row) => new()
    {
        Id = row.Field<int>("ID"),
        ProvinceCode = row.Field<string>("ProvinceCode") ?? string.Empty,
        ProvinceName = row.Field<string>("ProvinceName") ?? string.Empty,
        DataStatus = row.Field<string>("DataStatus") ?? string.Empty,
        CreateUserID = row.Field<int>("CreateUserID"),
        CreateUserDate = row.Field<DateTime>("CreateUserDate"),
        UpdateUserID = row.Field<int?>("UpdateUserID"),
        UpdateUserDate = row.Field<DateTime?>("UpdateUserDate")
    };

}
