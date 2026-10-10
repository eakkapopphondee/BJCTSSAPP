using Microsoft.AspNetCore.Mvc;
using thaibev.Data;
using thaibev.Models;

namespace thaibev.Controllers;

public sealed class LicensePlateController(LicensePlateService service) : Controller
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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> GenerateQRCode(int id, CancellationToken cancellationToken)
    {
        var exists = await service.GenerateQRCodeAsync(id, User, cancellationToken);
        if (!exists) return BadRequest(new { message = "กรุณาเพิ่มรายการสินค้าก่อนพิมพ์ QR Code" });
        return Ok(new { url = Url.Action(nameof(PrintQRCode), new { id }) });
    }

    [HttpGet, ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> PrintQRCode(int id, CancellationToken cancellationToken)
    {
        var plate = await service.FindPrintableAsync(id, cancellationToken);
        if (plate is null)
        {
            TempData["QRCodeWarning"] = "กรุณาเพิ่มรายการสินค้าก่อนพิมพ์ QR Code";
            return RedirectToAction(nameof(Index));
        }
        var png = QRCoder.PngByteQRCodeHelper.GetQRCode(plate.QRCode!, QRCoder.QRCodeGenerator.ECCLevel.Q, 12);
        return View(new LicensePlatePrintViewModel(plate, "data:image/png;base64," + Convert.ToBase64String(png), DateTime.Now));
    }

}
