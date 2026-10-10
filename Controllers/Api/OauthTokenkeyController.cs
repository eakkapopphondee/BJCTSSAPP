using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using thaibev.Data;
using thaibev.Infrastructure.Api;
using thaibev.Models;

namespace thaibev.Controllers.Api;

[ApiController]
[ApiAccess(AllowUnauthenticatedExternal = true)]
[Route("v1/Auth/Oauth/Tokenkey")]
public sealed class OauthTokenkeyController(OauthTokenkeyService service) : ControllerBase
{
    [HttpPost]
    [Consumes("application/json")]
    [ResponseCache(NoStore = true, Location = ResponseCacheLocation.None)]
    public async Task<IActionResult> Create([FromBody] OauthTokenkeyRequest request, CancellationToken cancellationToken)
    {
        var header = Request.Headers["AccessToken"];
        if (header.Count != 1 || string.IsNullOrWhiteSpace(header[0]))
            throw new ValidationException("AccessToken header is required.");
        var token = await service.CreateAsync(header[0]!, request, cancellationToken);
        return ApiResponse.Success(new { Token = token }).ToResult();
    }
}
