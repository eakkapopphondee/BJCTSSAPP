using System.ComponentModel.DataAnnotations;

namespace thaibev.Models;

public sealed class RoleViewModel : AuditedViewModel
{
    public int Id { get; set; }

    [Required, StringLength(20)]
    public string RoleCode { get; set; } = string.Empty;

    [Required, StringLength(200)]
    public string RoleDescription { get; set; } = string.Empty;

    [Required, StringLength(1)]
    public string DataStatus { get; set; } = "A";




}

