using Microsoft.AspNetCore.Mvc;
using thaibev.Data;
using thaibev.Models;

namespace thaibev.Controllers;

public sealed class ProgramTypeController(ProgramTypeService service) : Controller
{
    [HttpGet]
    public IActionResult Index() => View();

    [HttpGet]
    public async Task<IActionResult> Create(CancellationToken cancellationToken) =>
        View(await service.CreateModelAsync(cancellationToken));

    [HttpGet]
    public async Task<IActionResult> Edit(int id, CancellationToken cancellationToken)
    {
        var model = await service.FindAsync(id, cancellationToken);
        return model is null ? NotFound() : View(model);
    }

}
