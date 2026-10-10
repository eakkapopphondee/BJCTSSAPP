using System.ComponentModel.DataAnnotations;

namespace thaibev.Models;

public sealed class TruckTypeViewModel : AuditedViewModel
{
    public int Id { get; set; }

    [Required, StringLength(2)]
    public string TruckTypeCode { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string TruckTypeName { get; set; } = string.Empty;

    [Required, RegularExpression("^[YN]$")]
    public string DefaultFlag { get; set; } = "N";

    [Required, StringLength(1)]
    public string DataStatus { get; set; } = "A";




}

