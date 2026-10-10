using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using thaibev.Models;

namespace thaibev.Data;

public sealed partial class ProductService
{
    private string TableName => $"{sqlObjects.Prefix}.[tsssProduct]";
    private string TypeTable => $"{sqlObjects.Prefix}.[tsssProductType]";
    private string JoinedTable => $"(SELECT p.*, t.[ProductTypeName] FROM {TableName} p LEFT JOIN {TypeTable} t ON t.[ID] = p.[ProductTypeID]) product";
    private const string SelectColumns = """
        [ID], [ProductCode], [ProductName], [SAPCode], [SAPDesc], [ProductTypeID], [UnitPrice], [StdWeight], [TBRDefaultFlag], [Remark],
          [DataStatus], [CreateUserID], [CreateUserDate],
        [UpdateUserID], [UpdateUserDate]
        """;


    public async Task<ProductViewModel> CreateModelAsync(CancellationToken cancellationToken)
    {
        var model = new ProductViewModel();
        await LoadOptions(model, true, cancellationToken);
        return model;
    }

    public async Task<ProductViewModel?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync(
            $"SELECT {SelectColumns}, [ProductTypeName] FROM {JoinedTable} WHERE [ID] = @Id;",
            [new SqlParameter("@Id", SqlDbType.Int) { Value = id }], cancellationToken);
        if (table.Rows.Count == 0) return null;
        var model = ToModel(table.Rows[0]);
        await LoadOptions(model, false, cancellationToken);
        return model;
    }



    private async Task LoadOptions(ProductViewModel model, bool activeOnly, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync($"""
            SELECT [ID], [ProductTypeName] FROM {TypeTable}
            WHERE (@ActiveOnly = 0 OR [DataStatus] = 'A') ORDER BY [ProductTypeName], [ID];
            """, [new SqlParameter("@ActiveOnly", SqlDbType.Bit) { Value = activeOnly }], cancellationToken);
        model.ProductTypes = table.Rows.Cast<DataRow>().Select(row => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(
            row.Field<string>("ProductTypeName"), row.Field<int>("ID").ToString())).ToList();
        if (model.ProductTypeID > 0 && model.ProductTypes.All(item => item.Value != model.ProductTypeID.ToString()))
            model.ProductTypes.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(
                $"ไม่พบประเภทสินค้า (ID {model.ProductTypeID}) — กรุณาเลือกใหม่", model.ProductTypeID.ToString()) { Disabled = true });
    }



    private static ProductViewModel ToModel(DataRow row) => new()
    {
        Id = row.Field<int>("ID"),
        ProductCode = row.Field<string>("ProductCode") ?? string.Empty,
        ProductName = row.Field<string>("ProductName") ?? string.Empty,
        ProductTypeID = row.Field<int>("ProductTypeID"),
        ProductTypeName = row.Field<string>("ProductTypeName"),
        UnitPrice = row.Field<decimal>("UnitPrice"),
        StdWeight = row.Field<decimal>("StdWeight"),
        TBRDefaultFlag = row.Field<string>("TBRDefaultFlag") ?? string.Empty,
        SAPCode = row.Field<string?>("SAPCode"),
        SAPDesc = row.Field<string?>("SAPDesc"),
        Remark = row.Field<string?>("Remark"),
        DataStatus = row.Field<string>("DataStatus") ?? string.Empty,
        CreateUserID = row.Field<int>("CreateUserID"),
        CreateUserDate = row.Field<DateTime>("CreateUserDate"),
        UpdateUserID = row.Field<int?>("UpdateUserID"),
        UpdateUserDate = row.Field<DateTime?>("UpdateUserDate")
    };

}
