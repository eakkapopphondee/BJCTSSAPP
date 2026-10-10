using System.ComponentModel.DataAnnotations;

namespace thaibev.Models;

public class ProductListRequest
{
    public string? TextSearch { get; set; }
    [Required, RegularExpression("^[ACL]$")] public string DataStatus { get; set; } = "A";
}

public sealed class ProductSearchRequest : ProductListRequest
{
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 1000)] public int PerPage { get; set; } = 10;
}

public sealed class ProductDeleteRequest
{
    [Required, Range(1, int.MaxValue)] public int? ID { get; set; }
    public object? UpdateUserID { get; set; }
    public object? UpdateUserDate { get; set; }
}

public sealed record ProductPage(List<Dictionary<string, object?>> Rows, int Page, int PerPage, int TotalCount, int TotalPages);
