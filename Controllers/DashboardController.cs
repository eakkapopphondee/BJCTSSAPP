using Microsoft.AspNetCore.Mvc;

namespace thaibev.Controllers;

public sealed class DashboardController : Controller
{
    [HttpGet]
    public IActionResult Index() => View();
}
