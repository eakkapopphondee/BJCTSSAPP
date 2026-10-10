using System.ComponentModel.DataAnnotations;
using System.Text;
using System.Text.Json;
using thaibev.Infrastructure.Validation;

namespace thaibev.Infrastructure.Api;

public sealed record ExternalApiRequest(
    string? RequestRefKey, DateTime? RequestDate, int? RequestUserID, string? RequsetUserName,
    string RequestBody, DateTime SyncDate)
{
    public const string ContextKey = "Api.ExternalRequest";

    public static async Task<ExternalApiRequest> ReadAsync(HttpContext context, DateTime syncDate, ApiRouteOptions options)
    {
        var request = context.Request;
        request.EnableBuffering();
        string body;
        using (var reader = new StreamReader(request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: true,
            leaveOpen: true))
            body = await reader.ReadToEndAsync(context.RequestAborted);
        request.Body.Position = 0;
        if (!options.ValidateRequestMetadata)
            return ReadUnvalidatedMetadata(body, syncDate);
        if (string.IsNullOrWhiteSpace(body))
            throw new ValidationException("JSON request body is required for external API calls.");

        JsonDocument document;
        try { document = JsonDocument.Parse(body); }
        catch (JsonException error) { throw new ValidationException("Invalid JSON request body.", error); }
        using (document)
        {
            var fields = InputValidation.ReadObject(document.RootElement);
            var reference = RequiredText(fields, "RequestRefKey", 50);
            var userName = RequiredText(fields, "RequsetUserName", 100);
            if (!fields.TryGetValue("RequestUserID", out var userId)
                || userId.ValueKind != JsonValueKind.Number || !userId.TryGetInt32(out var parsedId))
                throw new ValidationException("RequestUserID must be an integer.");
            if (!fields.TryGetValue("RequestDate", out var date)
                || date.ValueKind != JsonValueKind.String || !date.TryGetDateTime(out var parsedDate))
                throw new ValidationException("RequestDate must be a datetime string.");
            InputValidation.ValidateSqlDateTime(parsedDate, "RequestDate");
            return new(reference, parsedDate, parsedId, userName, body, syncDate);
        }
    }

    private static ExternalApiRequest ReadUnvalidatedMetadata(string body, DateTime syncDate)
    {
        if (string.IsNullOrWhiteSpace(body)) return new(null, null, null, null, body, syncDate);
        try
        {
            using var document = JsonDocument.Parse(body);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return new(null, null, null, null, body, syncDate);
            var fields = document.RootElement.EnumerateObject()
                .GroupBy(field => field.Name, StringComparer.OrdinalIgnoreCase)
                .ToDictionary(group => group.Key, group => group.First().Value, StringComparer.OrdinalIgnoreCase);
            static string? Text(Dictionary<string, JsonElement> fields, string name, int maxLength) =>
                fields.TryGetValue(name, out var value) && value.ValueKind == JsonValueKind.String
                && !string.IsNullOrWhiteSpace(value.GetString()) && value.GetString()!.Length <= maxLength
                    ? value.GetString() : null;
            var reference = Text(fields, "RequestRefKey", 50);
            var userName = Text(fields, "RequsetUserName", 100);
            int? userId = fields.TryGetValue("RequestUserID", out var idValue)
                && idValue.ValueKind == JsonValueKind.Number && idValue.TryGetInt32(out var parsedId) ? parsedId : null;
            DateTime? date = fields.TryGetValue("RequestDate", out var dateValue)
                && dateValue.ValueKind == JsonValueKind.String && dateValue.TryGetDateTime(out var parsedDate)
                && parsedDate >= new DateTime(1753, 1, 1)
                && parsedDate <= new DateTime(9999, 12, 31, 23, 59, 59, 997) ? parsedDate : null;
            return new(reference, date, userId, userName, body, syncDate);
        }
        catch (JsonException)
        {
            return new(null, null, null, null, body, syncDate);
        }
    }

    private static string RequiredText(Dictionary<string, JsonElement> fields, string name, int maxLength)
    {
        if (!fields.TryGetValue(name, out var value) || value.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(value.GetString()))
            throw new ValidationException($"{name} is required and must be a string.");
        var text = value.GetString()!;
        if (text.Length > maxLength)
            throw new ValidationException($"{name} exceeds {maxLength} characters.");
        return text;
    }
}
