using System.ComponentModel.DataAnnotations;

namespace thaibev.Models;

public sealed class WeightTypeViewModel : AuditedViewModel
{
    public int Id { get; set; }

    [Required, StringLength(1)]
    public string WeightTypeCode { get; set; } = string.Empty;

    [Required, StringLength(80)]
    public string WeightTypeName { get; set; } = string.Empty;

    [Required, RegularExpression("^[AC]$", ErrorMessage = "สถานะต้องเป็น A หรือ C เท่านั้น")]
    public string DataStatus { get; set; } = "A";




}

