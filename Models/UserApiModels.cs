using System.ComponentModel.DataAnnotations;

namespace thaibev.Models;

public class UserListRequest
{
    public string? TextSearch { get; set; }
    [Required, RegularExpression("^[ACL]$")] public string DataStatus { get; set; } = "A";
}

public sealed class UserSearchRequest : UserListRequest
{
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 1000)] public int PerPage { get; set; } = 10;
}

public sealed class UserDeleteRequest
{
    [Required, Range(1, int.MaxValue)] public int? ID { get; set; }
    public object? UpdateUserID { get; set; }
    public object? UpdateUserDate { get; set; }
}

public sealed record UserPage(List<Dictionary<string, object?>> Rows, int Page, int PerPage, int TotalCount, int TotalPages);
