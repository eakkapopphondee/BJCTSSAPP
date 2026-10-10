namespace thaibev.Models;

public sealed record ProductTypeImportRowResult(int RowNumber, string? ProductTypeCode, bool Success, int? ID, string Message);

public sealed record ProductTypeImportResult(int TotalCount, int SuccessCount, int ErrorCount,
    List<ProductTypeImportRowResult> Rows);
