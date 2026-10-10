using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace thaibev.Models;

public sealed class LicensePlateViewModel : AuditedViewModel
{
    public int Id { get; set; }
    [Range(1, int.MaxValue, ErrorMessage = "กรุณาเลือกประเภทรถ")]
    public int TruckTypeID { get; set; }
    [StringLength(15)]
    public string? QRCode { get; set; }
    public string? TruckTypeName { get; set; }
    [ValidateNever]
    public List<SelectListItem> TruckTypes { get; set; } = [];
    [Required, StringLength(30)]
    public string PlateNumber { get; set; } = string.Empty;
    [Range(1, int.MaxValue, ErrorMessage = "กรุณาเลือกจังหวัดทะเบียนรถ")]
    public int PlateProvinceID { get; set; }
    [Required, StringLength(30)]
    public string TrailerNumber { get; set; } = string.Empty;
    [Range(1, int.MaxValue, ErrorMessage = "กรุณาเลือกจังหวัดทะเบียนพ่วง")]
    public int TrailerProviceID { get; set; }
    [Range(1, int.MaxValue)]
    public int? TransportCompanyID { get; set; }
    [Required, StringLength(1)]
    public string DataStatus { get; set; } = "A";

    public string? PlateProvinceName { get; set; }
    public string? TrailerProvinceName { get; set; }
    public string? TransportCompanyName { get; set; }
    [ValidateNever]
    public List<SelectListItem> Provinces { get; set; } = [];
    [ValidateNever]
    public List<SelectListItem> TransportCompanies { get; set; } = [];
}
