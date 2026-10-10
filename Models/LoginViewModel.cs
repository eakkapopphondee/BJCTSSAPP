using System.ComponentModel.DataAnnotations;

namespace thaibev.Models;

public sealed class LoginViewModel
{
    [Required(ErrorMessage = "กรุณากรอกชื่อผู้ใช้")]
    [StringLength(50)]
    public string Username { get; set; } = string.Empty;

    [Required(ErrorMessage = "กรุณากรอกรหัสผ่าน"), DataType(DataType.Password)]
    public string Password { get; set; } = string.Empty;
}
