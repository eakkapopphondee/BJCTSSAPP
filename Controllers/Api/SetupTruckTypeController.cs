using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using thaibev.Data;
using thaibev.Infrastructure.Api;
using thaibev.Infrastructure.Validation;
using thaibev.Models;

namespace thaibev.Controllers.Api;

[ApiController]
[ApiAccess]
[Route("v1/Setup/TruckType")]
public sealed class SetupTruckTypeController(TruckTypeService service) : ControllerBase
{
    private bool External => HttpContext.Items[ApiAccessFilter.ExternalKey] is true;

    [HttpGet("Search")]
    public async Task<IActionResult> Search([FromQuery] TruckTypeSearchRequest request, CancellationToken cancellationToken) =>
        TruckTypeService.Response(await service.SearchAsync(request, cancellationToken)).ToResult();

    [HttpGet("List"), ExternalOnly]
    public async Task<IActionResult> List([FromQuery] TruckTypeListRequest request, CancellationToken cancellationToken) =>
        TruckTypeService.Response(await service.ListAsync(request, cancellationToken)).ToResult();

    [HttpPost("Save"), Consumes("application/json")]
    public async Task<IActionResult> Save([FromBody] JsonElement body, CancellationToken cancellationToken) =>
        ApiResponse.Success(await service.SaveAsync(body, External ? null : User, cancellationToken)).ToResult();

    [HttpDelete("Delete"), Consumes("application/json")]
    public async Task<IActionResult> Delete([FromBody] TruckTypeDeleteRequest request, CancellationToken cancellationToken)
    {
        InputValidation.ValidateModel(request);
        return ApiResponse.Success(await service.DeleteAsync(request.ID!.Value,
            External ? InputValidation.OptionalInt(request.UpdateUserID, "UpdateUserID") : AuditStamp.UserId(User),
            External ? InputValidation.OptionalDate(request.UpdateUserDate, "UpdateUserDate") : DateTime.Now, cancellationToken)).ToResult();
    }
}
