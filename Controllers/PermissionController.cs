using Microsoft.AspNetCore.Mvc;
using thaibev.Data;

namespace thaibev.Controllers;

public sealed class PermissionController(RoleService roles) : Controller
{
    [HttpGet] public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var role = await roles.FindAsync(id, cancellationToken);
        return role is null ? NotFound() : View(role);
    }
}
