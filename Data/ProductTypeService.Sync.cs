using System.Text.Json;

namespace thaibev.Data;

public sealed partial class ProductTypeService
{
    public Task<object> SyncAsync(JsonElement body, CancellationToken token) =>
        SetupSync.RunAsync(body, "ProductTypeSyncRequest",
            ["SAPCode", "SAPDesc", "Remark", "DataStatus", "CreateUserID"],
            async (values, ct) =>
            {
                values["ProductTypeCode"] = values["SAPCode"];
                values["ProductTypeName"] = values["SAPDesc"];
                return await SaveAsync(JsonSerializer.SerializeToElement(values), null, ct);
            }, token);
}
