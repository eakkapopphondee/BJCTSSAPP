using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using thaibev.Data;

namespace thaibev.ViewComponents;

public sealed class FactoryBrandLogoViewComponent(FactoryService factories, ILogger<FactoryBrandLogoViewComponent> logger) : ViewComponent
{
    public async Task<IViewComponentResult> InvokeAsync(bool mainPage = false)
    {
        var path = FactoryService.DefaultBrandLogoPath;
        try { path = await factories.GetBrandLogoPathAsync(HttpContext.RequestAborted); }
        catch (SqlException error)
        {
            // A branding lookup failure must not prevent the login page from rendering.
            logger.LogWarning(error, "Unable to load the active factory logo.");
        }
        if (mainPage && path == FactoryService.DefaultBrandLogoPath)
            path = "~/images/logo-default-main.svg";
        var source = Url.Content(path.StartsWith("~/", StringComparison.Ordinal) ? path : "~/" + path.TrimStart('/'));
        return View(mainPage ? "MainPage" : "Default", source);
    }
}
