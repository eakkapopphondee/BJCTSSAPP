using Microsoft.AspNetCore.Mvc;
using thaibev.Data;
using thaibev.Models;
using thaibev.Infrastructure.Api;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;

namespace thaibev.Controllers;

public sealed class ProductTypeController(ProductTypeService service) : Controller
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

    [HttpPost, ValidateAntiForgeryToken, StandardApiResponse]
    [RequestSizeLimit(10_000_000)]
    public async Task<IActionResult> ParseImport(IFormFile? file, CancellationToken cancellationToken)
    {
        if (file is null || file.Length is < 1 or > 5_000_000 ||
            !Path.GetExtension(file.FileName).Equals(".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Select a product type .xlsx file up to 5 MB.");
        await using var stream = file.OpenReadStream();
        var rows = ProductTypeExcelParser.Parse(stream);
        cancellationToken.ThrowIfCancellationRequested();
        return ApiResponse.Success(rows).ToResult();
    }

    [HttpPost, ValidateAntiForgeryToken, StandardApiResponse, Consumes("application/json")]
    public async Task<IActionResult> Import([FromBody] JsonElement body, CancellationToken cancellationToken) =>
        ApiResponse.Success(await service.ImportAsync(body, User, cancellationToken)).ToResult();

}
