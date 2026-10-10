namespace thaibev.Models;

public sealed record ProductImportRowResult(int RowNumber, string? ProductCode, bool Success, int? ID, string Message);

public sealed record ProductImportResult(int TotalCount, int SuccessCount, int ErrorCount,
    List<ProductImportRowResult> Rows);
