using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using thaibev.Models;

namespace thaibev.Data;

public sealed partial class LicensePlateService
{
    private string TableName => $"{sqlObjects.Prefix}.[tsssLicensePlate]";
    // Use the same province table as ProvinceController. TrailerProviceID is the database column spelling.
    private string ProvinceTable => $"{sqlObjects.Prefix}.[tsssProvince]";
    private string CompanyTable => $"{sqlObjects.Prefix}.[tsssTransportCompany]";
    private string TruckTypeTable => $"{sqlObjects.Prefix}.[tsssTruckType]";
    private string FromClause => $"""
        FROM {TableName} l
        LEFT JOIN {ProvinceTable} p ON p.[ID] = l.[PlateProvinceID]
        LEFT JOIN {ProvinceTable} t ON t.[ID] = l.[TrailerProviceID]
        LEFT JOIN {CompanyTable} c ON c.[ID] = l.[TransportCompanyID]
        LEFT JOIN {TruckTypeTable} tt ON tt.[ID] = l.[TruckTypeID]
        """;
    private const string Columns = """
        l.[ID], l.[PlateNumber], l.[PlateProvinceID], l.[TrailerNumber], l.[TrailerProviceID],
        l.[TransportCompanyID], l.[DataStatus], l.[CreateUserID], l.[CreateUserDate], l.[UpdateUserID], l.[UpdateUserDate],
        p.[ProvinceName] AS PlateProvinceName, t.[ProvinceName] AS TrailerProvinceName,
        c.[TransportCompanyName], l.[TruckTypeID], l.[QRCode], tt.[TruckTypeName]
        """;


    public async Task<LicensePlateViewModel> CreateModelAsync(CancellationToken cancellationToken)
    {
        var model = new LicensePlateViewModel();
        await LoadOptions(model, true, cancellationToken);
        return model;
    }

    public async Task<LicensePlateViewModel?> FindAsync(int id, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync($"SELECT {Columns} {FromClause} WHERE l.[ID] = @Id;",
            [new SqlParameter("@Id", SqlDbType.Int) { Value = id }], cancellationToken);
        if (table.Rows.Count == 0) return null;
        var model = ToModel(table.Rows[0]);
        await LoadOptions(model, false, cancellationToken);
        return model;
    }

    public async Task<bool> GenerateQRCodeAsync(int id, System.Security.Claims.ClaimsPrincipal user, CancellationToken cancellationToken)
    {
        var code = "SPC" + id.ToString(System.Globalization.CultureInfo.InvariantCulture);
        var affected = await dataAccess.UpdateAsync($"""
            SET XACT_ABORT ON;
            BEGIN TRY
                BEGIN TRANSACTION;
                UPDATE {TableName}
                SET [QRCode]=@QRCode, [UpdateUserID]=@UserID, [UpdateUserDate]=@Now
                WHERE [ID]=@Id AND EXISTS (
                    SELECT 1 FROM {sqlObjects.Prefix}.[tsssLicensePlateProduct] WITH (UPDLOCK, HOLDLOCK)
                    WHERE [LicensePlateID]=@Id AND [DataStatus]='A');
                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH;
            """, [
                new("@Id", SqlDbType.Int) { Value = id },
                new("@QRCode", SqlDbType.VarChar, 15) { Value = code },
                new("@UserID", SqlDbType.Int) { Value = AuditStamp.UserId(user) },
                new("@Now", SqlDbType.DateTime) { Value = DateTime.Now }
            ], cancellationToken);
        return affected > 0;
    }

    public async Task<LicensePlateViewModel?> FindPrintableAsync(int id, CancellationToken cancellationToken)
    {
        var table = await dataAccess.SelectAsync($"""
            SELECT {Columns} {FromClause} WHERE l.[ID]=@Id AND l.[QRCode]=@QRCode
                AND EXISTS (SELECT 1 FROM {sqlObjects.Prefix}.[tsssLicensePlateProduct]
                    WHERE [LicensePlateID]=@Id AND [DataStatus]='A');
            """, [
                new("@Id", SqlDbType.Int) { Value = id },
                new("@QRCode", SqlDbType.VarChar, 15) { Value = "SPC" + id.ToString(System.Globalization.CultureInfo.InvariantCulture) }
            ], cancellationToken);
        return table.Rows.Count == 0 ? null : ToModel(table.Rows[0]);
    }


    private async Task LoadOptions(LicensePlateViewModel model, bool activeOnly, CancellationToken cancellationToken)
    {
        var truckTypes = await dataAccess.SelectAsync($"""
            SELECT [ID], [TruckTypeName] FROM {TruckTypeTable}
            WHERE [DataStatus] = 'A' OR (@ActiveOnly = 0 AND [ID] = @TruckTypeID)
            ORDER BY [TruckTypeName], [ID];
            """, [
                new SqlParameter("@ActiveOnly", SqlDbType.Bit) { Value = activeOnly },
                new SqlParameter("@TruckTypeID", SqlDbType.Int) { Value = model.TruckTypeID }
            ], cancellationToken);
        model.TruckTypes = truckTypes.Rows.Cast<DataRow>().Select(row => new SelectListItem(
            row.Field<string>("TruckTypeName"), row.Field<int>("ID").ToString())).ToList();
        AddMissingOption(model.TruckTypes, model.TruckTypeID);
        // Create lists active records only; Edit retains existing inactive selections.
        var provinces = await dataAccess.SelectAsync($"""
            SELECT [ID], [ProvinceName] FROM {ProvinceTable}
            WHERE (@ActiveOnly = 0 OR [DataStatus] = 'A') ORDER BY [ProvinceName], [ID];
            """, [new SqlParameter("@ActiveOnly", SqlDbType.Bit) { Value = activeOnly }], cancellationToken);
        model.Provinces = provinces.Rows.Cast<DataRow>().Select(row => new SelectListItem(
            row.Field<string>("ProvinceName"), row.Field<int>("ID").ToString())).ToList();
        var companies = await dataAccess.SelectAsync($"""
            SELECT [ID], [TransportCompanyName] FROM {CompanyTable}
            WHERE (@ActiveOnly = 0 OR [DataStatus] = 'A') ORDER BY [TransportCompanyName], [ID];
            """, [new SqlParameter("@ActiveOnly", SqlDbType.Bit) { Value = activeOnly }], cancellationToken);
        model.TransportCompanies = companies.Rows.Cast<DataRow>().Select(row => new SelectListItem(
            row.Field<string>("TransportCompanyName"), row.Field<int>("ID").ToString())).ToList();
        AddMissingOption(model.Provinces, model.PlateProvinceID);
        AddMissingOption(model.Provinces, model.TrailerProviceID);
        AddMissingOption(model.TransportCompanies, model.TransportCompanyID);
    }

    private static void AddMissingOption(List<SelectListItem> options, int? id)
    {
        if (id is > 0 && options.All(option => option.Value != id.Value.ToString()))
            options.Add(new SelectListItem($"ไม่พบข้อมูลอ้างอิง (ID {id}) — กรุณาเลือกใหม่", id.Value.ToString()) { Disabled = true });
    }

    private static LicensePlateViewModel ToModel(DataRow row) => new()
    {
        TruckTypeID = row.Field<int>("TruckTypeID"), QRCode = row.Field<string>("QRCode"),
        TruckTypeName = row.Field<string>("TruckTypeName"),
        Id = row.Field<int>("ID"), PlateNumber = row.Field<string>("PlateNumber") ?? string.Empty,
        PlateProvinceID = row.Field<int>("PlateProvinceID"), TrailerNumber = row.Field<string>("TrailerNumber") ?? string.Empty,
        TrailerProviceID = row.Field<int>("TrailerProviceID"), TransportCompanyID = row.Field<int?>("TransportCompanyID"),
        DataStatus = row.Field<string>("DataStatus") ?? string.Empty, CreateUserID = row.Field<int>("CreateUserID"),
        CreateUserDate = row.Field<DateTime>("CreateUserDate"), UpdateUserID = row.Field<int?>("UpdateUserID"),
        UpdateUserDate = row.Field<DateTime?>("UpdateUserDate"), PlateProvinceName = row.Field<string>("PlateProvinceName"),
        TrailerProvinceName = row.Field<string>("TrailerProvinceName"), TransportCompanyName = row.Field<string>("TransportCompanyName")
    };

}
