using Microsoft.AspNetCore.Mvc;
using thaibev.Data;
using thaibev.Infrastructure.Api;
using thaibev.Models;

namespace thaibev.Controllers.Api;

[ApiController]
[ApiAccess]
[Route("v1/Auth/Permission")]
public sealed class SetupPermissionController(PermissionService service) : ControllerBase
{
    [HttpGet("Search")]
    public async Task<IActionResult> Search([FromQuery] PermissionSearchRequest request, CancellationToken cancellationToken) =>
        (await service.SearchAsync(request, cancellationToken)).ToResult();

    [HttpGet("List")]
    public async Task<IActionResult> List([FromQuery] PermissionListRequest request, CancellationToken cancellationToken) =>
        ApiResponse.Success(await service.ListAsync(request, cancellationToken)).ToResult();

    [HttpPost("Save"), Consumes("application/json")]
    public async Task<IActionResult> Save([FromBody] PermissionSaveRequest request, CancellationToken cancellationToken) =>
        ApiResponse.Success(await service.SaveAsync(request,
            HttpContext.Items[ApiAccessFilter.ExternalKey] is true ? null : User, cancellationToken)).ToResult();
}
