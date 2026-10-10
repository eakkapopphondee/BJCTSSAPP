using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using Microsoft.Data.SqlClient;


namespace thaibev.Infrastructure.Api;

// Keep every opted-in API endpoint's errors JSON, including authentication, binding and infrastructure errors.
public sealed class ApiErrors(RequestDelegate next, ILogger<ApiErrors> logger)
{
    public static bool IsDataRequest(HttpRequest request) =>
        request.HttpContext.GetEndpoint()?.Metadata.GetMetadata<ApiAccessAttribute>() is not null
        || request.HttpContext.GetEndpoint()?.Metadata.GetMetadata<StandardApiResponseAttribute>() is not null
        // Unmatched/versioned API requests still receive JSON 404/405 errors.
        || request.Path.StartsWithSegments("/v1", StringComparison.OrdinalIgnoreCase);

    public async Task InvokeAsync(HttpContext context, ExternalApiAudit audit)
    {
        if (!IsDataRequest(context.Request)) { await next(context); return; }
        var original = context.Response.Body;
        await using var buffer = new MemoryStream();
        context.Response.Body = buffer;
        try
        {
            try { await next(context); }
            catch (OperationCanceledException) when (context.RequestAborted.IsCancellationRequested) { throw; }
            catch (Exception error)
            {
                var (code, message) = error switch
                {
                    ApiException apiError => (apiError.StatusCode, apiError.Message),
                    ValidationException => (400, error.Message),
                    SqlException { Number: 547 } => (400, "A referenced record does not exist or is still in use."),
                    SqlException { Number: 2601 or 2627 } => (409, "A record with these values already exists."),
                    BadHttpRequestException => (400, "Invalid request body."),
                    _ => (500, "Unable to process the API request.")
                };
                if (code == 500) logger.LogError(error, "API request failed");
                context.Response.Clear();
                buffer.SetLength(0);
                context.Response.StatusCode = code;
                await context.Response.WriteAsJsonAsync(ApiResponse.Failure(message, HttpMethods.IsGet(context.Request.Method)));
            }
            if (context.Response.StatusCode >= 400 && !IsStandard(buffer))
            {
                var code = context.Response.StatusCode;
                var validationMessage = code == 400 ? ValidationSummary(buffer) : null;
                buffer.SetLength(0);
                var message = validationMessage ?? (code switch
                {
                    400 => "Invalid request or missing antiforgery token.",
                    401 => "Web login or a valid external token is required.",
                    403 => "Access denied.", 404 => "Endpoint or record was not found.",
                    405 => "HTTP method is not allowed.", 415 => "Unsupported request content type.",
                    _ => "Unable to process the API request."
                });
                context.Response.ContentLength = null;
                await context.Response.WriteAsJsonAsync(ApiResponse.Failure(message, HttpMethods.IsGet(context.Request.Method)));
            }
            if (context.Items[ExternalApiRequest.ContextKey] is ExternalApiRequest externalRequest)
            {
                try { await audit.CompleteAsync(context, buffer, externalRequest); }
                catch (Exception error)
                {
                    logger.LogError(error, "Unable to log the external API request");
                    context.Response.Clear();
                    buffer.SetLength(0);
                    context.Response.StatusCode = 500;
                    await context.Response.WriteAsJsonAsync(ApiResponse.Failure("Unable to log the external API request.", HttpMethods.IsGet(context.Request.Method)));
                }
            }
            buffer.Position = 0;
            await buffer.CopyToAsync(original, context.RequestAborted);
        }
        finally { context.Response.Body = original; }
    }

    private static bool IsStandard(MemoryStream buffer)
    {
        try
        {
            using var json = JsonDocument.Parse(buffer.ToArray());
            return json.RootElement.ValueKind == JsonValueKind.Object
                && json.RootElement.TryGetProperty("StatusCode", out var code)
                && code.ValueKind == JsonValueKind.String && code.GetString() == "F001";
        }
        catch (JsonException) { return false; }
    }

    private static string? ValidationSummary(MemoryStream buffer)
    {
        try
        {
            using var json = JsonDocument.Parse(buffer.ToArray());
            if (json.RootElement.ValueKind != JsonValueKind.Object
                || !json.RootElement.TryGetProperty("errors", out var errors)
                || errors.ValueKind != JsonValueKind.Object) return null;
            var messages = errors.EnumerateObject().Where(error => error.Value.ValueKind == JsonValueKind.Array)
                .SelectMany(error => error.Value.EnumerateArray())
                .Where(error => error.ValueKind == JsonValueKind.String).Select(error => error.GetString());
            var summary = string.Join(" ", messages);
            return string.IsNullOrWhiteSpace(summary) ? null : summary;
        }
        catch (JsonException) { return null; }
    }
}
