using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace thaibev.Models;

// Audit values are readable in list responses, but cannot be supplied by JSON or form clients.
public abstract class AuditedViewModel
{
    [BindNever]
    public int CreateUserID { get; internal set; }
    [BindNever]
    public DateTime CreateUserDate { get; internal set; }
    [BindNever]
    public int? UpdateUserID { get; internal set; }
    [BindNever]
    public DateTime? UpdateUserDate { get; internal set; }
}
