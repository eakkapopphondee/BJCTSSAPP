using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using thaibev.Models;
using thaibev.Data;

namespace thaibev.Controllers;

public sealed class AccountController(UserAuthentication authentication, PasswordHash passwordHash) : Controller
{
    [AllowAnonymous, HttpGet]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> SessionStatus()
    {
        var ticket = await HttpContext.AuthenticateAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        if (!ticket.Succeeded || ticket.Properties?.ExpiresUtc is not { } expires)
            return Unauthorized();
        return SessionDetails(ticket.Properties, expires);
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> RenewSession(
        [FromServices] Microsoft.Extensions.Options.IOptionsMonitor<CookieAuthenticationOptions> options)
    {
        var scheme = CookieAuthenticationDefaults.AuthenticationScheme;
        var ticket = await HttpContext.AuthenticateAsync(scheme);
        var now = DateTimeOffset.UtcNow;
        if (!ticket.Succeeded || ticket.Principal is null || ticket.Properties?.ExpiresUtc is not { } expires || expires <= now)
            return Unauthorized();

        var properties = ticket.Properties;
        properties.IssuedUtc = now;
        properties.ExpiresUtc = now.Add(options.Get(scheme).ExpireTimeSpan);
        await HttpContext.SignInAsync(scheme, ticket.Principal, properties);
        return SessionDetails(properties, properties.ExpiresUtc.Value);
    }

    private IActionResult SessionDetails(AuthenticationProperties properties, DateTimeOffset expires)
    {
        var now = DateTimeOffset.UtcNow;
        var lifetime = (expires - (properties.IssuedUtc ?? now)).TotalSeconds;
        return Json(new
        {
            remainingSeconds = Math.Max(0, (expires - now).TotalSeconds),
            warningSeconds = Math.Min(300, Math.Max(1, lifetime / 2))
        });
    }

    [AllowAnonymous, HttpGet]
    public IActionResult Login()
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Dashboard");
        return View(new LoginViewModel());
    }

    [AllowAnonymous, HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model, CancellationToken cancellationToken)
    {
        if (ModelState.IsValid)
        {
            var principal = await authentication.AuthenticateHashAsync(
                model.Username, passwordHash.Compute(model.Password), cancellationToken);
            if (principal is not null)
            {
                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme,
                    principal, new AuthenticationProperties { IsPersistent = false });
                return RedirectToAction("Index", "Dashboard");
            }
        }

        if (ModelState.IsValid) ModelState.AddModelError(string.Empty, "ชื่อผู้ใช้หรือรหัสผ่านไม่ถูกต้อง");
        model.Password = string.Empty;
        ModelState.Remove(nameof(model.Password));
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction(nameof(Login));
    }
}
