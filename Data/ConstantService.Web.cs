using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using thaibev.Models;

namespace thaibev.Data;

public sealed partial class ConstantService
{
    private string TableName => $"{sqlObjects.Prefix}.[tsssConstant]";
    private string FactoryTable => $"{sqlObjects.Prefix}.[tsssFactory]";
    // FactoryCode is not declared unique; joining distinct codes prevents duplicate constant rows.
    private string JoinedTable => $"(SELECT c.*, f.[FactoryCode] AS JoinedFactoryCode FROM {TableName} c LEFT JOIN (SELECT DISTINCT [FactoryCode] FROM {FactoryTable}) f ON f.[FactoryCode] = c.[FactoryCode]) constants";
    private const string SelectColumns = """
        [ID], [FactoryCode], [ConstantCode], [ConstantName], [ConstantValue], [Activate], [ShowInCardFlag], [ShowInProgramFlag],
          [DataStatus], [CreateUserID], [CreateUserDate],
        [UpdateUserID], [UpdateUserDate]
        """;


    public async Task<ConstantViewModel> CreateModelAsync(CancellationToken cancellationToken)
    {
        var model = new ConstantViewModel();
        await LoadOptions(model, true, cancellationToken);
        return model;
    }

    public async Task<ConstantViewModel?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync(
            $"SELECT {SelectColumns}, [JoinedFactoryCode] FROM {JoinedTable} WHERE [ID] = @Id;",
            [new SqlParameter("@Id", SqlDbType.Int) { Value = id }], cancellationToken);
        if (table.Rows.Count == 0) return null;
        var model = ToModel(table.Rows[0]);
        await LoadOptions(model, false, cancellationToken);
        return model;
    }



    private async Task LoadOptions(ConstantViewModel model, bool activeOnly, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync($"""
            SELECT DISTINCT [FactoryCode] FROM {FactoryTable}
            WHERE (@ActiveOnly = 0 OR [DataStatus] = 'A') ORDER BY [FactoryCode];
            """, [new SqlParameter("@ActiveOnly", SqlDbType.Bit) { Value = activeOnly }], cancellationToken);
        model.Factories = table.Rows.Cast<DataRow>().Select(row => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(
            row.Field<string>("FactoryCode"), row.Field<string>("FactoryCode"))).ToList();
        if (!string.IsNullOrEmpty(model.FactoryCode) && model.Factories.All(item => item.Value != model.FactoryCode))
            model.Factories.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(
                $"{model.FactoryCode} (ไม่พบโรงงาน — กรุณาเลือกใหม่)", model.FactoryCode) { Disabled = true });
    }



    private static ConstantViewModel ToModel(DataRow row) => new()
    {
        Id = row.Field<int>("ID"),
        ConstantCode = row.Field<string>("ConstantCode") ?? string.Empty,
        ConstantName = row.Field<string>("ConstantName") ?? string.Empty,
        FactoryCode = row.Field<string>("FactoryCode") ?? string.Empty,
        JoinedFactoryCode = row.Field<string>("JoinedFactoryCode"),
        Activate = row.Field<string>("Activate") ?? "N",
        ShowInCardFlag = row.Field<string>("ShowInCardFlag") ?? "N",
        ShowInProgramFlag = row.Field<string>("ShowInProgramFlag") ?? "N",
        ConstantValue = row.Field<string>("ConstantValue") ?? string.Empty,
        DataStatus = row.Field<string>("DataStatus") ?? string.Empty,
        CreateUserID = row.Field<int>("CreateUserID"),
        CreateUserDate = row.Field<DateTime>("CreateUserDate"),
        UpdateUserID = row.Field<int?>("UpdateUserID"),
        UpdateUserDate = row.Field<DateTime?>("UpdateUserDate")
    };

}
