using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc;

namespace thaibev.Infrastructure.Api;

public sealed record ApiResponse(
    [property: JsonPropertyName("Status")] string Status,
    [property: JsonPropertyName("StatusCode")] string StatusCode,
    [property: JsonPropertyName("Message")] string Message,
    [property: JsonPropertyName("Data")] object Data)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = null };

    [JsonExtensionData]
    public Dictionary<string, object?>? Metadata { get; init; }

    public static ApiResponse Success(object data, Dictionary<string, object?>? metadata = null) =>
        new("S", "S001", "Success", data) { Metadata = metadata };

    public static ApiResponse Failure(string message, bool get) =>
        new("F", "F001", message, get ? Array.Empty<object>() : new { });

    public JsonResult ToResult(int statusCode = 200) => new(this, JsonOptions) { StatusCode = statusCode };
}

// Mark JSON adapters that retain normal cookie authentication (rather than ApiAccess).
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = true)]
public sealed class StandardApiResponseAttribute : Attribute { }

public sealed class ApiException(int statusCode, string message, Exception? inner = null) : Exception(message, inner)
{
    public int StatusCode { get; } = statusCode;
}
