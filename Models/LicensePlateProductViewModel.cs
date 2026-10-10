using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace thaibev.Models;

public sealed class LicensePlateProductViewModel : IValidatableObject
{
    public int Id { get; set; }
    [Required, RegularExpression("^[AC]$")]
    public string DataStatus { get; set; } = "A";
    [Range(1, int.MaxValue)]
    public int LicensePlateID { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "กรุณาเลือกสินค้า")]
    public int ProductID { get; set; }
    [Required(ErrorMessage = "กรุณาระบุวันที่เริ่มใช้")]
    public DateTime? EffDateFrom { get; set; }
    [Required(ErrorMessage = "กรุณาระบุวันที่สิ้นสุด")]
    public DateTime? EffDateTo { get; set; }
    [ValidateNever] public string? PlateNumber { get; set; }
    [ValidateNever] public string? PlateProvinceName { get; set; }
    [ValidateNever] public string? TrailerNumber { get; set; }
    [ValidateNever] public string? TrailerProvinceName { get; set; }
    [ValidateNever] public string? TransportCompanyName { get; set; }
    [ValidateNever] public List<SelectListItem> Products { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext context)
    {
        if (EffDateFrom > EffDateTo)
            yield return new ValidationResult("วันที่สิ้นสุดต้องไม่น้อยกว่าวันที่เริ่มใช้", [nameof(EffDateTo)]);
        if (EffDateFrom < new DateTime(1753, 1, 1) || EffDateTo < new DateTime(1753, 1, 1))
            yield return new ValidationResult("วันที่ต้องไม่ก่อนปี 1753", [nameof(EffDateFrom), nameof(EffDateTo)]);
    }
}
