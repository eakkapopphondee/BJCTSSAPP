using System.ComponentModel.DataAnnotations;
using System.Data;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace thaibev.Data;

public sealed partial class ProductService
{
    public Task<object> SyncAsync(JsonElement body, CancellationToken token) =>
        SetupSync.RunAsync(body, "ProductSyncRequest",
            ["ProductTypeCode", "ProductTypeName", "SAPCode", "SAPDesc", "Remark", "DataStatus", "CreateUserID"],
            async (values, ct) =>
            {
                var code = (string)values["ProductTypeCode"]!;
                if (string.IsNullOrWhiteSpace(code) || code.Length > 20)
                    throw new ValidationException("ProductTypeCode must be 1 to 20 characters.");
                var matches = await dataAccess.SelectAsync($"SELECT [ID] FROM {sqlObjects.Prefix}.[tsssProductType] WHERE [ProductTypeCode]=@ProductTypeCode AND [DataStatus]='A';",
                    [new SqlParameter("@ProductTypeCode", SqlDbType.NVarChar, 20) { Value = code }], ct);
                if (matches.Rows.Count != 1) throw new ValidationException("ProductTypeCode must reference one active product type.");
                values.Remove("ProductTypeCode");
                values.Remove("ProductTypeName");
                values["ProductTypeID"] = Convert.ToInt32(matches.Rows[0]["ID"]);
                values["ProductCode"] = values["SAPCode"];
                values["ProductName"] = values["SAPDesc"];
                values["UnitPrice"] = 0;
                values["StdWeight"] = 0;
                values["TBRDefaultFlag"] = "N";
                return await SaveAsync(JsonSerializer.SerializeToElement(values), null, ct);
            }, token);
}
