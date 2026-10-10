using Microsoft.AspNetCore.Mvc;
using thaibev.Data;

namespace thaibev.Controllers;

public sealed class LicensePlateProductController(LicensePlateProductService service) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index(int id, CancellationToken cancellationToken)
    {
        var model = await service.FindForPlateAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }
}
