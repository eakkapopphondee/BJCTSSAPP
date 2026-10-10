using System.Text.Json;

namespace thaibev.Data;

public sealed partial class CustomerService
{
    public Task<object> SyncAsync(JsonElement body, CancellationToken token) =>
        SetupSync.RunAsync(body, "CustomerSyncRequest",
            ["SAPCode", "SAPDesc", "Address1", "PhoneNumber", "Remark", "MolasFlag", "TBRFlag", "DefaultFlag", "DataStatus", "CreateUserID"],
            async (values, ct) =>
            {
                values["CustomerCode"] = values["SAPCode"];
                values["CustomerName"] = values["SAPDesc"];
                return await SaveAsync(JsonSerializer.SerializeToElement(values), null, ct);
            }, token);
}
