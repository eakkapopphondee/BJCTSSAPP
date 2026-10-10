using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using thaibev.Models;

namespace thaibev.Data;

public sealed partial class ProductTypeService
{
    private string TableName => $"{sqlObjects.Prefix}.[tsssProductType]";
    private const string SelectColumns = """
        [ID], [ProductTypeCode], [ProductTypeName], [SAPCode], [SAPDesc], [Remark],
          [DataStatus], [CreateUserID], [CreateUserDate],
        [UpdateUserID], [UpdateUserDate]
        """;


    public Task<ProductTypeViewModel> CreateModelAsync(CancellationToken cancellationToken) => Task.FromResult(new ProductTypeViewModel());

    public async Task<ProductTypeViewModel?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync(
            $"SELECT {SelectColumns} FROM {TableName} WHERE [ID] = @Id;",
            [new SqlParameter("@Id", SqlDbType.Int) { Value = id }], cancellationToken);
        return table.Rows.Count == 0 ? null : ToModel(table.Rows[0]);
    }



    private static ProductTypeViewModel ToModel(DataRow row) => new()
    {
        Id = row.Field<int>("ID"),
        ProductTypeCode = row.Field<string>("ProductTypeCode") ?? string.Empty,
        ProductTypeName = row.Field<string>("ProductTypeName") ?? string.Empty,
        SAPCode = row.Field<string>("SAPCode"),
        SAPDesc = row.Field<string>("SAPDesc"),
        Remark = row.Field<string>("Remark") ?? string.Empty,
        DataStatus = row.Field<string>("DataStatus") ?? string.Empty,
        CreateUserID = row.Field<int>("CreateUserID"),
        CreateUserDate = row.Field<DateTime>("CreateUserDate"),
        UpdateUserID = row.Field<int?>("UpdateUserID"),
        UpdateUserDate = row.Field<DateTime?>("UpdateUserDate")
    };

}
