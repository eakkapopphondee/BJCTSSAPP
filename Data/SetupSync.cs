using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using thaibev.Infrastructure.Validation;

namespace thaibev.Data;

public static class SetupSync
{
    public static async Task<object> RunAsync(JsonElement body, string node, string[] fields,
        Func<Dictionary<string, object?>, CancellationToken, Task<Dictionary<string, object?>>> save,
        CancellationToken cancellationToken)
    {
        var root = InputValidation.ReadObject(body);
        if (!root.TryGetValue(node, out var rows) || rows.ValueKind != JsonValueKind.Array)
            throw new ValidationException($"{node} must be a JSON array.");
        if (rows.GetArrayLength() is < 1 or > 5000)
            throw new ValidationException($"{node} requires 1 to 5000 rows.");

        var results = new List<object>();
        var success = 0;
        var index = 0;
        foreach (var row in rows.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            index++;
            try
            {
                var input = InputValidation.ReadObject(row);
                foreach (var key in input.Keys)
                    if (!fields.Contains(key, StringComparer.OrdinalIgnoreCase))
                        throw new ValidationException($"Unexpected field: {key}.");
                var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var field in fields)
                {
                    if (!input.TryGetValue(field, out var value))
                    {
                        if (field == "DefaultFlag" && node == "CustomerSyncRequest")
                        {
                            values[field] = "N";
                            continue;
                        }
                        if (field == "Remark" && node != "ProductTypeSyncRequest")
                        {
                            values[field] = null;
                            continue;
                        }
                        throw new ValidationException($"{field} is required.");
                    }
                    if (field == "CreateUserID")
                    {
                        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var userId))
                            throw new ValidationException("CreateUserID must be an integer.");
                        values[field] = userId;
                    }
                    else
                    {
                        if (field == "Remark" && node != "ProductTypeSyncRequest" && value.ValueKind == JsonValueKind.Null)
                        {
                            values[field] = null;
                            continue;
                        }
                        if (value.ValueKind != JsonValueKind.String)
                            throw new ValidationException($"{field} must be a string.");
                        values[field] = value.GetString();
                    }
                }
                values["CreateUserDate"] = DateTime.Now;
                var saved = await save(values, cancellationToken);
                success++;
                results.Add(new { RowNumber = index, Success = true, ID = saved["ID"], Message = "Success" });
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception error) when (error is ValidationException or Infrastructure.Api.ApiException or Microsoft.Data.SqlClient.SqlException)
            {
                results.Add(new { RowNumber = index, Success = false, ID = (object?)null, Message = error.Message });
            }
        }
        return new { TotalCount = index, SuccessCount = success, ErrorCount = index - success, Rows = results };
    }
}
