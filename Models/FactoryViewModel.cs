using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace thaibev.Models;

public sealed class FactoryViewModel : AuditedViewModel
{
    public int Id { get; set; }

    [Microsoft.AspNetCore.Mvc.ModelBinding.BindNever, StringLength(250)]
    public string? LogoPath { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public IFormFile? LogoPathFile { get; set; }

    [Required, StringLength(20)]
    public string FactoryCode { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string FactoryDesc { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Address1 { get; set; } = string.Empty;

    [StringLength(200)]
    public string? Address2 { get; set; }

    [StringLength(200)]
    public string? Address3 { get; set; }

    [Required, StringLength(10)]
    public string BranchCode { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string BranchDesc { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string PhoneNumber { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string SAPCode { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string SAPDesc { get; set; } = string.Empty;

    [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired, Required, Range(1, int.MaxValue)]
    public int ProgramTypeID { get; set; }

    public string? ProgramTypeCode { get; set; }

    public string? ProgramTypeName { get; set; }

    public List<SelectListItem> ProgramTypes { get; set; } = [];

    [Required, RegularExpression("^[YN]$")]
    public string PrintFlag { get; set; } = string.Empty;

    [Microsoft.AspNetCore.Mvc.ModelBinding.BindRequired, Required]
    public int PrintCopy { get; set; }

    [Required, RegularExpression("^[AC]$")]
    public string DataStatus { get; set; } = string.Empty;




}
