using System.ComponentModel.DataAnnotations;
namespace thaibev.Models;
public sealed class URLSettingViewModel : AuditedViewModel
{
    public int Id { get; set; }
    [Required, StringLength(20)]
    public string FactoryCode { get; set; } = string.Empty;

    [Required, StringLength(20)]
    public string URLSettingCode { get; set; } = string.Empty;

    [Required, StringLength(3), RegularExpression("^(In|Out)$", ErrorMessage = "InOut ต้องเป็น In หรือ Out เท่านั้น")]
    public string InOut { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string URLSettingName { get; set; } = string.Empty;

    [Required, StringLength(10)]
    public string Method { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string ContentType { get; set; } = string.Empty;

    [Required, StringLength(100)]
    public string Header1Type { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Header1Value { get; set; } = string.Empty;

    [StringLength(100)]
    public string? Header2Type { get; set; }

    [StringLength(200)]
    public string? Header2Value { get; set; }

    [StringLength(100)]
    public string? Header3Type { get; set; }

    [StringLength(200)]
    public string? Header3Value { get; set; }

    [Required, StringLength(250)]
    public string URL { get; set; } = string.Empty;

    [Required, StringLength(1)]
    public string DataStatus { get; set; } = "A";



    public string? JoinedFactoryCode { get; set; }
    [Microsoft.AspNetCore.Mvc.ModelBinding.Validation.ValidateNever]
    public List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem> Factories { get; set; } = [];
}




