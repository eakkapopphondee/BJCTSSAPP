using System.ComponentModel.DataAnnotations;

namespace thaibev.Models;

public sealed class ConstantViewModel : AuditedViewModel
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string ConstantCode { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string ConstantName { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string ConstantValue { get; set; } = string.Empty;

    [Required, StringLength(1)]
    public string DataStatus { get; set; } = "A";




    [Required, StringLength(20)]
    public string FactoryCode { get; set; } = string.Empty;
    [Required, StringLength(1), RegularExpression("^[YN]$")]
    public string Activate { get; set; } = "N";
    [Required, StringLength(1), RegularExpression("^[YN]$")]
    public string ShowInCardFlag { get; set; } = "N";
    [Required, StringLength(1), RegularExpression("^[YN]$")]
    public string ShowInProgramFlag { get; set; } = "N";
    public string? JoinedFactoryCode { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Factories { get; set; } = [];
}



