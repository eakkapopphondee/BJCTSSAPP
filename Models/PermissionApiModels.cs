using System.ComponentModel.DataAnnotations;

namespace thaibev.Models;

public class PermissionListRequest
{
    [Range(1, int.MaxValue)] public int RoleID { get; set; }
}

public sealed class PermissionSearchRequest : PermissionListRequest
{
    [Range(1, int.MaxValue)] public int Page { get; set; } = 1;
    [Range(1, 1000)] public int PerPage { get; set; } = 10;
}

public sealed class PermissionSaveRequest : PermissionListRequest
{
    [Required, MinLength(1)] public List<PermissionItem>? Permissions { get; set; }
    public int? UpdateUserID { get; set; }
    public DateTime? UpdateUserDate { get; set; }
}

public sealed class PermissionItem
{
    [Range(1, int.MaxValue)] public int MenuID { get; set; }
    [Required, RegularExpression("^[YN]$")] public string AccessFlag { get; set; } = "N";
    [Required, RegularExpression("^[YN]$")] public string ExportFlag { get; set; } = "N";
}
