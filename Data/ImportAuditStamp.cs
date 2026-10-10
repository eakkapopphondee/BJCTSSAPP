using System.Text.Json;
using thaibev.Infrastructure.Validation;

namespace thaibev.Data;

public static class ImportAuditStamp
{
    public static JsonElement WithCreateDate(JsonElement row)
    {
        var input = InputValidation.ReadObject(row);
        if (input.ContainsKey("CreateUserDate")) return row;
        var values = input.ToDictionary(pair => pair.Key, pair => (object?)pair.Value,
            StringComparer.OrdinalIgnoreCase);
        values["CreateUserDate"] = DateTime.Now;
        return JsonSerializer.SerializeToElement(values);
    }
}
