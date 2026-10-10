using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using thaibev.Data;
using thaibev.Infrastructure.Api;
using thaibev.Infrastructure.Validation;
using thaibev.Models;

namespace thaibev.Controllers.Api;

[ApiController]
[ApiAccess]
[Route("v1/Setup/Factory")]
public sealed class SetupFactoryController(FactoryService service) : ControllerBase
{
    private bool External => HttpContext.Items[ApiAccessFilter.ExternalKey] is true;

    [HttpGet("Search")]
    public async Task<IActionResult> Search([FromQuery] FactorySearchRequest request, CancellationToken cancellationToken) =>
        FactoryService.Response(await service.SearchAsync(request, cancellationToken)).ToResult();

    [HttpGet("List"), ExternalOnly]
    public async Task<IActionResult> List([FromQuery] FactoryListRequest request, CancellationToken cancellationToken) =>
        FactoryService.Response(await service.ListAsync(request, cancellationToken)).ToResult();

    [HttpPost("Save"), Consumes("application/json")]
    public async Task<IActionResult> Save([FromBody] JsonElement body, CancellationToken cancellationToken) =>
        ApiResponse.Success(await service.SaveAsync(body, External ? null : User, cancellationToken)).ToResult();

    [HttpDelete("Delete"), Consumes("application/json")]
    public async Task<IActionResult> Delete([FromBody] FactoryDeleteRequest request, CancellationToken cancellationToken)
    {
        InputValidation.ValidateModel(request);
        return ApiResponse.Success(await service.DeleteAsync(request.ID!.Value,
            External ? InputValidation.OptionalInt(request.UpdateUserID, "UpdateUserID") : AuditStamp.UserId(User),
            External ? InputValidation.OptionalDate(request.UpdateUserDate, "UpdateUserDate") : DateTime.Now, cancellationToken)).ToResult();
    }

    [HttpPost("Save"), Consumes("multipart/form-data")]
    public async Task<IActionResult> SaveForm([FromForm] FactoryViewModel model, CancellationToken cancellationToken)
    {
        if (External) return ApiResponse.Failure("External Save requests must use application/json.", false).ToResult(415);
        return ApiResponse.Success(await service.SaveFormAsync(model, User, cancellationToken)).ToResult();
    }
}
