using System.Security.Claims;
using thaibev.Models;

namespace thaibev.Data;

public static class AuditStamp
{
    public static bool IsAuditField(string name) => name is "CreateUserID" or "CreateUserDate" or "UpdateUserID" or "UpdateUserDate";

    public static int UserId(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true &&
        int.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) && id > 0
            ? id : throw new InvalidOperationException("A valid signed-in user ID is required.");

    public static void Apply(ClaimsPrincipal user, AuditedViewModel model, bool edit)
    {
        var id = UserId(user);
        var now = DateTime.Now;
        // Creation parameters are also sent on updates, but are never assigned by UPDATE SQL.
        model.CreateUserID = id;
        model.CreateUserDate = now;
        model.UpdateUserID = edit ? id : null;
        model.UpdateUserDate = edit ? now : null;
    }
}
