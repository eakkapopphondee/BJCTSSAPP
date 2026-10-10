using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using thaibev.Models;

namespace thaibev.Data;

public sealed partial class URLSettingService
{
    private string TableName => $"{sqlObjects.Prefix}.[tsssURLSetting]";
    private string FactoryTable => $"{sqlObjects.Prefix}.[tsssFactory]";
    // FactoryCode is not declared unique; joining distinct codes prevents duplicate URL setting rows.
    private string JoinedTable => $"(SELECT c.*, f.[FactoryCode] AS JoinedFactoryCode FROM {TableName} c LEFT JOIN (SELECT DISTINCT [FactoryCode] FROM {FactoryTable}) f ON f.[FactoryCode] = c.[FactoryCode]) settings";
    private const string SelectColumns = "[ID], [FactoryCode], [URLSettingCode], [InOut], [URLSettingName], [Method], [ContentType], [Header1Type], [Header1Value], [Header2Type], [Header2Value], [Header3Type], [Header3Value], [URL], [DataStatus], [CreateUserID], [CreateUserDate], [UpdateUserID], [UpdateUserDate]";


    public async Task<URLSettingViewModel> CreateModelAsync(CancellationToken cancellationToken)
    {
        var model = new URLSettingViewModel();
        await LoadOptions(model, true, cancellationToken);
        return model;
    }

    public async Task<URLSettingViewModel?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync(
            $"SELECT {SelectColumns}, [JoinedFactoryCode] FROM {JoinedTable} WHERE [ID] = @Id;",
            [new SqlParameter("@Id", SqlDbType.Int) { Value = id }], cancellationToken);
        if (table.Rows.Count == 0) return null;
        var model = ToModel(table.Rows[0]);
        await LoadOptions(model, false, cancellationToken);
        return model;
    }



    private async Task LoadOptions(URLSettingViewModel model, bool activeOnly, CancellationToken cancellationToken)
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

    private static URLSettingViewModel ToModel(DataRow row) => new()
    {
        Id = row.Field<int>("ID"),
        JoinedFactoryCode = row.Field<string>("JoinedFactoryCode"),
        FactoryCode = row.Field<string>("FactoryCode") ?? string.Empty,
        URLSettingCode = row.Field<string>("URLSettingCode") ?? string.Empty,
        InOut = row.Field<string>("InOut") ?? string.Empty,
        URLSettingName = row.Field<string>("URLSettingName") ?? string.Empty,
        Method = row.Field<string>("Method") ?? string.Empty,
        ContentType = row.Field<string>("ContentType") ?? string.Empty,
        Header1Type = row.Field<string>("Header1Type") ?? string.Empty,
        Header1Value = row.Field<string>("Header1Value") ?? string.Empty,
        Header2Type = row.Field<string>("Header2Type"),
        Header2Value = row.Field<string>("Header2Value"),
        Header3Type = row.Field<string>("Header3Type"),
        Header3Value = row.Field<string>("Header3Value"),
        URL = row.Field<string>("URL") ?? string.Empty,
        DataStatus = row.Field<string>("DataStatus") ?? string.Empty,
        CreateUserID = row.Field<int>("CreateUserID"),
        CreateUserDate = row.Field<DateTime>("CreateUserDate"),
        UpdateUserID = row.Field<int?>("UpdateUserID"),
        UpdateUserDate = row.Field<DateTime?>("UpdateUserDate")
    };

}
