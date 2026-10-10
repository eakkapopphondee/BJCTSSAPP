using System.ComponentModel.DataAnnotations;

namespace thaibev.Models;

public sealed class CustomerViewModel : AuditedViewModel
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string CustomerCode { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string CustomerName { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Address1 { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(50)]
    public string? SAPCode { get; set; }

    [StringLength(250)]
    public string? SAPDesc { get; set; }

    [StringLength(1024)]
    public string? Remark { get; set; }

    [Required, RegularExpression("^[YN]$")]
    public string MolasFlag { get; set; } = "N";

    [Required, RegularExpression("^[YN]$")]
    public string TBRFlag { get; set; } = "N";

    [Required, RegularExpression("^[YN]$")]
    public string DefaultFlag { get; set; } = "N";

    [Required, StringLength(1)]
    public string DataStatus { get; set; } = "A";




}
