using System.ComponentModel.DataAnnotations;

namespace thaibev.Models;

public sealed class NotificationViewModel : AuditedViewModel
{
    public int Id { get; set; }
    [Required, StringLength(20)]
    public string NotificationNo { get; set; } = string.Empty;
    [Required, Range(typeof(DateTime), "1753-01-01", "9999-12-31")]
    public DateTime? NotificationDate { get; set; } = DateTime.Now;
    [Required, StringLength(150)]
    public string NotificationName { get; set; } = string.Empty;
    [Required, StringLength(200)]
    public string NotificationSubj { get; set; } = string.Empty;
    public string? NotificationMsg { get; set; }
    [Required, RegularExpression("^[DW]$", ErrorMessage = "TriggerType must be Daily or Weekly.")]
    public string TriggerType { get; set; } = string.Empty;
    [Required, Range(typeof(DateTime), "1753-01-01", "9999-12-31")]
    public DateTime? StartDate { get; set; } = DateTime.Now;
    [Required, Range(typeof(TimeSpan), "00:00:00", "23:59:59.9999999")]
    public TimeSpan? NotificationTime { get; set; }
    [Required, RegularExpression("^[AC]$", ErrorMessage = "สถานะต้องเป็น A หรือ C เท่านั้น")]
    public string DataStatus { get; set; } = "A";
    [Required]
    public List<NotificationRecipientViewModel> ToRecipients { get; set; } = [];
    [Required]
    public List<NotificationRecipientViewModel> CcRecipients { get; set; } = [];
}

public sealed class NotificationRecipientViewModel
{
    [Range(0, int.MaxValue)]
    public int Id { get; set; }
    [Required, EmailAddress, StringLength(200)]
    public string Email { get; set; } = string.Empty;
    [Required, StringLength(200)]
    public string Owner { get; set; } = string.Empty;
}
