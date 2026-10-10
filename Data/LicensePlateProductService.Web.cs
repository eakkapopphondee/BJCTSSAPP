using System.Data;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Data.SqlClient;
using thaibev.Models;

namespace thaibev.Data;

public sealed partial class LicensePlateProductService
{
    private string Prefix => sqlObjects.Prefix;
    public async Task<LicensePlateProductViewModel?> FindForPlateAsync(int id, CancellationToken cancellationToken)
    {
        var model = new LicensePlateProductViewModel { LicensePlateID = id, EffDateFrom = DateTime.Today, EffDateTo = DateTime.Today };
        return await Load(model, cancellationToken) ? model : null;
    }
    private async Task<bool> Load(LicensePlateProductViewModel model, CancellationToken cancellationToken)
    {
        var header = await dataAccess.SelectAsync($"""
            SELECT l.[PlateNumber], p.[ProvinceName] AS PlateProvinceName, l.[TrailerNumber],
                t.[ProvinceName] AS TrailerProvinceName, c.[TransportCompanyName]
            FROM {Prefix}.[tsssLicensePlate] l
            LEFT JOIN {Prefix}.[tsssProvince] p ON p.[ID]=l.[PlateProvinceID]
            LEFT JOIN {Prefix}.[tsssProvince] t ON t.[ID]=l.[TrailerProviceID]
            LEFT JOIN {Prefix}.[tsssTransportCompany] c ON c.[ID]=l.[TransportCompanyID]
            WHERE l.[ID]=@Id;
            """, [new("@Id", SqlDbType.Int) { Value = model.LicensePlateID }], cancellationToken);
        if (header.Rows.Count == 0) return false;
        var row = header.Rows[0];
        model.PlateNumber = row.Field<string>("PlateNumber");
        model.PlateProvinceName = row.Field<string>("PlateProvinceName");
        model.TrailerNumber = row.Field<string>("TrailerNumber");
        model.TrailerProvinceName = row.Field<string>("TrailerProvinceName");
        model.TransportCompanyName = row.Field<string>("TransportCompanyName");
        var products = await dataAccess.SelectAsync($"SELECT [ID], [ProductCode], [ProductName] FROM {Prefix}.[tsssProduct] WHERE [DataStatus]='A' ORDER BY [ProductCode], [ID];", [], cancellationToken);
        model.Products = products.Rows.Cast<DataRow>().Select(r => new SelectListItem(
            $"{r.Field<string>("ProductCode")} - {r.Field<string>("ProductName")}", r.Field<int>("ID").ToString())).ToList();
        return true;
    }
}
