using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using System.Text.Json;
using Microsoft.Data.SqlClient;
using thaibev.Infrastructure.Api;
using thaibev.Infrastructure.Validation;
using thaibev.Models;

namespace thaibev.Data;

public sealed partial class ProductTypeService
{
    public async Task<ProductTypeImportResult> ImportAsync(JsonElement body, ClaimsPrincipal? user, CancellationToken cancellationToken)
    {
        if (body.ValueKind != JsonValueKind.Array)
            throw new ValidationException("Import body must be a JSON array.");
        if (body.GetArrayLength() is < 1 or > 5000)
            throw new ValidationException("Import requires 1 to 5000 rows.");
        var results = new List<ProductTypeImportRowResult>(body.GetArrayLength());
        var position = 0;
        foreach (var row in body.EnumerateArray())
        {
            cancellationToken.ThrowIfCancellationRequested();
            position++;
            var excelRow = position + 1;
            string? code = null;
            try
            {
                var input = InputValidation.ReadObject(row);
                if (input.TryGetValue("ExcelRow", out var rowValue) && rowValue.TryGetInt32(out var providedRow) && providedRow > 1)
                    excelRow = providedRow;
                if (input.TryGetValue("ProductTypeCode", out var codeValue) && codeValue.ValueKind == JsonValueKind.String)
                    code = codeValue.GetString();
                if (input.TryGetValue("ID", out var idValue) && idValue.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined) &&
                    !(idValue.ValueKind == JsonValueKind.Number && idValue.TryGetInt32(out var id) && id == 0))
                    throw new ValidationException("Import only creates product types; ID must be omitted or zero.");
                var saved = await SaveAsync(ImportAuditStamp.WithCreateDate(row), user, cancellationToken);
                results.Add(new(excelRow, code, true, Convert.ToInt32(saved["ID"]), "สำเร็จ"));
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
            catch (Exception error)
            {
                var message = error switch
                {
                    ValidationException or ApiException => error.Message,
                    SqlException { Number: 2601 or 2627 } => "รหัสหรือข้อมูลประเภทสินค้าซ้ำในฐานข้อมูล",
                    SqlException { Number: 547 } => "ข้อมูลอ้างอิงไม่ถูกต้อง",
                    _ => "ไม่สามารถบันทึกแถวนี้ได้"
                };
                results.Add(new(excelRow, code, false, null, message));
            }
        }
        return new(results.Count, results.Count(row => row.Success), results.Count(row => !row.Success), results);
    }
}
