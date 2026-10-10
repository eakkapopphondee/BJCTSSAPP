using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using thaibev.Data;
using thaibev.Infrastructure.Api;

namespace thaibev.Controllers.Api;

[ApiController]
[ApiAccess(AllowUnauthenticatedExternal = true)]
[Route("v1/Auth/Oauth/Access")]
public sealed class OauthAccessController(AccessTokenService service) : ControllerBase
{
    [HttpPost]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Create(CancellationToken cancellationToken)
    {
        var address = HttpContext.Connection.RemoteIpAddress
            ?? throw new ValidationException("Request IP address is unavailable.");
        if (address.IsIPv4MappedToIPv6) address = address.MapToIPv4();
        var agent = Request.Headers.UserAgent.ToString();
        var code = await service.CreateAsync(address.ToString(),
            string.IsNullOrWhiteSpace(agent) ? null : agent, cancellationToken);
        return ApiResponse.Success(new { Code = code }).ToResult();
    }
}
