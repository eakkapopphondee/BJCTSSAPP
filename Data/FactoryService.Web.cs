using System.Data;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using thaibev.Models;
using thaibev.Infrastructure.Validation;

namespace thaibev.Data;

// Factory-specific page models and upload handling.
public sealed partial class FactoryService
{
    public const string DefaultBrandLogoPath = "~/images/logo-default.svg";

    public async Task<string> GetBrandLogoPathAsync(CancellationToken cancellationToken)
    {
        // Count active factories, including those without a logo. Never pick one arbitrarily.
        var table = await dataAccess.SelectAsync($"""
            SELECT TOP (2) [LogoPath] FROM {TableName} WHERE [DataStatus] = 'A';
            """, [], cancellationToken);
        if (table.Rows.Count != 1) return DefaultBrandLogoPath;
        var path = table.Rows[0].Field<string>("LogoPath")?.Trim();
        return string.IsNullOrWhiteSpace(path) ? DefaultBrandLogoPath : path;
    }

    private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };
    private static FactoryViewModel Model(object record)
    {
        var json = JsonSerializer.SerializeToElement(record);
        var model = json.Deserialize<FactoryViewModel>(Json)!;
        // Audit properties deliberately have internal setters to prevent client binding.
        model.CreateUserID = json.GetProperty("CreateUserID").GetInt32();
        model.CreateUserDate = json.GetProperty("CreateUserDate").GetDateTime();
        model.UpdateUserID = json.GetProperty("UpdateUserID").Deserialize<int?>();
        model.UpdateUserDate = json.GetProperty("UpdateUserDate").Deserialize<DateTime?>();
        return model;
    }
    public async Task<Dictionary<string, object?>> SaveFormAsync(FactoryViewModel model, ClaimsPrincipal webUser, CancellationToken cancellationToken)
    {
        InputValidation.ValidateModel(model);
        string? uploaded = null;
        try
        {
            if (model.LogoPathFile is not null) model.LogoPath = uploaded = await logoStorage.SaveAsync(model.LogoPathFile, cancellationToken);
            return await SaveWebAsync(model, webUser, cancellationToken);
        }
        catch { logoStorage.DeleteNewUpload(uploaded); throw; }
    }
    private string TableName => Entity.Table(sqlObjects.Prefix);
    private string ProgramTypeTable => $"{sqlObjects.Prefix}.[tsssProgramType]";
    private static string SelectColumns => Entity.ColumnList;
    public async Task<FactoryViewModel> CreateModelAsync(CancellationToken cancellationToken)
    {
        var model = new FactoryViewModel { PrintFlag = "Y", DataStatus = "A" };
        await LoadProgramTypes(model, cancellationToken);
        return model;
    }

    private async Task LoadProgramTypes(FactoryViewModel model, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync($"""
            SELECT [ID], [ProgramTypeName] FROM {ProgramTypeTable}
            WHERE [DataStatus] = 'A' ORDER BY [ProgramTypeName], [ID];
            """, [], cancellationToken);
        model.ProgramTypes = table.Rows.Cast<DataRow>().Select(row => new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem(
            row.Field<string>("ProgramTypeName"), row.Field<int>("ID").ToString())).ToList();
    }

    public async Task<FactoryViewModel?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync(
            $"SELECT {SelectColumns} FROM {TableName} WHERE [Id] = @Id;",
            [new SqlParameter("@Id", id)],
            cancellationToken);

        if (table.Rows.Count == 0) return null;
        var model = ToModel(table.Rows[0]);
        await LoadProgramTypes(model, cancellationToken);
        return model;
    }


    private static FactoryViewModel ToModel(DataRow row) => Model(SqlValues.ToRecord(row));
}
