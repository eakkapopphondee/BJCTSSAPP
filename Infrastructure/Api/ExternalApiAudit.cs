using System.Data;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Data.SqlClient;
using thaibev.Data;

namespace thaibev.Infrastructure.Api;

public sealed class ExternalApiAudit(ISqlDataAccess dataAccess, SqlObjectNames sqlObjects)
{
    public async Task CompleteAsync(HttpContext context, MemoryStream buffer, ExternalApiRequest request)
    {
        var endpoint = context.Request.Path.Value ?? string.Empty;
        if (endpoint.Length > 250) throw new InvalidOperationException("API route exceeds tsssAPILog.APIEndPoint length.");
        var response = JsonNode.Parse(buffer.ToArray())?.AsObject()
            ?? throw new InvalidOperationException("External API response must be a JSON object.");
        var status = response["Status"]?.GetValue<string>()
            ?? throw new InvalidOperationException("External API response is missing Status.");
        var statusCode = response["StatusCode"]?.GetValue<string>()
            ?? throw new InvalidOperationException("External API response is missing StatusCode.");
        var message = response["Message"]?.GetValue<string>()
            ?? throw new InvalidOperationException("External API response is missing Message.");
        var finishedDate = DateTime.Now;

        var table = $"{sqlObjects.Prefix}.[tsssAPILog]";
        var sql = $"""
            SET XACT_ABORT ON; SET NOCOUNT ON;
            BEGIN TRY
                BEGIN TRANSACTION;
                IF EXISTS (SELECT 1 FROM {table} WITH (TABLOCKX, HOLDLOCK) WHERE [ResponseRefKey]=@ResponseRefKey)
                    THROW 51001, 'Generated response reference already exists', 1;
                INSERT INTO {table}
                    ([APIEndPoint], [RequestRefKey], [RequestDate], [RequsetUserName], [RequestBody],
                     [ResponseStatus], [ResponseStatusCode], [ResponseMessage], [ResponseData],
                     [ResponseRefKey], [ResponseDate])
                VALUES (@APIEndPoint, @RequestRefKey, @RequestDate, @RequsetUserName, @RequestBody,
                    @ResponseStatus, @ResponseStatusCode, @ResponseMessage, @ResponseData,
                    @ResponseRefKey, @ResponseDate);
                COMMIT TRANSACTION;
            END TRY
            BEGIN CATCH
                IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;
                THROW;
            END CATCH;
            """;
        for (var attempt = 0; attempt < 3; attempt++)
        {
            var reference = Guid.NewGuid().ToString("D");
            // GET endpoints return an array; give it a named place inside Data before adding audit fields.
            var data = response["Data"] is JsonObject objectData
                ? (JsonObject)objectData.DeepClone()
                : new JsonObject { ["Rows"] = response["Data"]?.DeepClone() };
            data["RequestRefKey"] = request.RequestRefKey;
            data["TSSRefKey"] = reference;
            data["SyncDate"] = request.SyncDate.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            data["FinishedDate"] = finishedDate.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);
            response["Data"] = data;
            var responseData = response["Data"]!.ToJsonString();
            try
            {
                await dataAccess.InsertAsync(sql,
                [
                    new("@APIEndPoint", SqlDbType.VarChar, 250) { Value = endpoint },
                    new("@RequestRefKey", SqlDbType.VarChar, 50) { Value = (object?)request.RequestRefKey ?? DBNull.Value },
                    new("@RequestDate", SqlDbType.DateTime) { Value = (object?)request.RequestDate ?? DBNull.Value },
                    new("@RequsetUserName", SqlDbType.NVarChar, 100) { Value = (object?)request.RequsetUserName ?? DBNull.Value },
                    new("@RequestBody", SqlDbType.NVarChar, -1) { Value = request.RequestBody },
                    new("@ResponseStatus", SqlDbType.VarChar, 10) { Value = status },
                    new("@ResponseStatusCode", SqlDbType.VarChar, 10) { Value = statusCode },
                    new("@ResponseMessage", SqlDbType.NVarChar, -1) { Value = message },
                    new("@ResponseData", SqlDbType.NVarChar, -1) { Value = responseData },
                    new("@ResponseRefKey", SqlDbType.VarChar, 50) { Value = reference },
                    new("@ResponseDate", SqlDbType.DateTime) { Value = finishedDate }
                ], context.RequestAborted);

                buffer.SetLength(0);
                await JsonSerializer.SerializeAsync(buffer, response, cancellationToken: context.RequestAborted);
                return;
            }
            catch (SqlException error) when (attempt < 2 && error.Number is 51001 or 2601 or 2627)
            {
                // Retry a UUID collision before emitting a response.
            }
        }
        throw new InvalidOperationException("Unable to create a unique response reference.");
    }
}
