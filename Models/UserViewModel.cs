using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Text.Json.Serialization;

namespace thaibev.Models;

public class UserViewModel : AuditedViewModel
{
    public int Id { get; set; }
    [Required, StringLength(150)]
    public string UserFullName { get; set; } = string.Empty;
    [Required, StringLength(50)]
    public string UserName { get; set; } = string.Empty;
    [EmailAddress, StringLength(80)]
    public string? Email { get; set; }
    [Range(1, int.MaxValue)]
    public int RoleID { get; set; }
    public string? RoleDescription { get; set; }
    [JsonIgnore]
    public List<SelectListItem> Roles { get; set; } = [];
    [Required, RegularExpression("^[AC]$")]
    public string DataStatus { get; set; } = "A";
}

public sealed class ChangeUserPasswordViewModel
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
    [Required, JsonIgnore, DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
    [Required, JsonIgnore, Compare(nameof(Password), ErrorMessage = "รหัสผ่านและยืนยันรหัสผ่านไม่ตรงกัน")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
