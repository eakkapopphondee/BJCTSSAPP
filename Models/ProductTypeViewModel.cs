using System.ComponentModel.DataAnnotations;

namespace thaibev.Models;

public sealed class ProductTypeViewModel : AuditedViewModel
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string ProductTypeCode { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string ProductTypeName { get; set; } = string.Empty;

    [StringLength(50)]
    public string? SAPCode { get; set; }

    [StringLength(250)]
    public string? SAPDesc { get; set; }

    [Required, StringLength(250)]
    public string Remark { get; set; } = string.Empty;

    [Required, StringLength(1)]
    public string DataStatus { get; set; } = "A";




}


