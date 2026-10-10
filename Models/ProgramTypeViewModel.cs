using System.ComponentModel.DataAnnotations;

namespace thaibev.Models;

public sealed class ProgramTypeViewModel : AuditedViewModel
{
    public int Id { get; set; }

    [Required, StringLength(1)]
    public string ProgramTypeCode { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string ProgramTypeName { get; set; } = string.Empty;

    [Required, RegularExpression("^[AC]$", ErrorMessage = "สถานะต้องเป็น A หรือ C เท่านั้น")]
    public string DataStatus { get; set; } = "A";




}

