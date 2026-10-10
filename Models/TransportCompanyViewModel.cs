using System.ComponentModel.DataAnnotations;

namespace thaibev.Models;

public sealed class TransportCompanyViewModel : AuditedViewModel
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string TransportCompanyCode { get; set; } = string.Empty;

    [Required, StringLength(250)]
    public string TransportCompanyName { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string Address1 { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string PhoneNumber { get; set; } = string.Empty;

    [StringLength(1024)]
    public string? Remark { get; set; }

    [Required, StringLength(1)]
    public string DataStatus { get; set; } = "A";




}

