using System.ComponentModel.DataAnnotations;

namespace thaibev.Models;

public sealed class ProvinceViewModel : AuditedViewModel
{
    public int Id { get; set; }

    [Required, StringLength(2)]
    public string ProvinceCode { get; set; } = string.Empty;

    [Required, StringLength(50)]
    public string ProvinceName { get; set; } = string.Empty;

    [Required, StringLength(1)]
    public string DataStatus { get; set; } = "A";




}

