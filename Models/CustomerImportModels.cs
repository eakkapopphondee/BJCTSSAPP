namespace thaibev.Models;

public sealed record CustomerImportRowResult(int RowNumber, string? CustomerCode, bool Success, int? ID, string Message);

public sealed record CustomerImportResult(int TotalCount, int SuccessCount, int ErrorCount,
    List<CustomerImportRowResult> Rows);
