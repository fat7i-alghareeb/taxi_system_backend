using Asp.Versioning;

using MediatR;

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

using Taxi.Application.Features.Audit.Queries.GetAuditLogs;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/audit-logs")]
[Authorize(Roles = "Admin")]
public class AuditLogsController(ISender sender) : ApiController
{
    [HttpGet]
    [ProducesResponseType(typeof(List<AuditLogDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Returns recent platform audit log entries.")]
    [EndpointName("GetAuditLogs")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 15,
        CancellationToken ct = default)
    {
        var result = await sender.Send(new GetAuditLogsQuery(page, pageSize), ct);

        return result.Match(
            Ok,
            Problem);
    }
}