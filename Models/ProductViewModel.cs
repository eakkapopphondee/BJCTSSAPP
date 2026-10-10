using System.ComponentModel.DataAnnotations;

namespace thaibev.Models;

public sealed class ProductViewModel : AuditedViewModel, IValidatableObject
{
    public int Id { get; set; }

    [Required, StringLength(30)]
    public string ProductCode { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string ProductName { get; set; } = string.Empty;

    [StringLength(250)]
    public string? Remark { get; set; }

    [StringLength(50)]
    public string? SAPCode { get; set; }

    [StringLength(250)]
    public string? SAPDesc { get; set; }

    [Required, StringLength(1)]
    public string DataStatus { get; set; } = "A";




    [Range(1, int.MaxValue, ErrorMessage = "กรุณาเลือกประเภทสินค้า")]
    public int ProductTypeID { get; set; }
    [Range(typeof(decimal), "-999999999.999", "999999999.999")]
    public decimal UnitPrice { get; set; }
    [Range(typeof(decimal), "-999999999.999", "999999999.999")]
    public decimal StdWeight { get; set; }
    [Required, StringLength(1)]
    public string TBRDefaultFlag { get; set; } = string.Empty;
    public string? ProductTypeName { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> ProductTypes { get; set; } = [];

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (decimal.Round(UnitPrice, 3) != UnitPrice)
            yield return new ValidationResult("ราคาต่อหน่วยต้องมีทศนิยมไม่เกิน 3 ตำแหน่ง", [nameof(UnitPrice)]);
        if (decimal.Round(StdWeight, 3) != StdWeight)
            yield return new ValidationResult("น้ำหนักมาตรฐานต้องมีทศนิยมไม่เกิน 3 ตำแหน่ง", [nameof(StdWeight)]);
    }
}


