using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Features.Uploads.Commands.UploadCompensationEvidence;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/uploads")]
public class UploadsController(ISender sender) : ApiController
{
    [HttpPost("compensation-evidence")]
    [Authorize(Roles = "Passenger,Admin")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Uploads compensation-claim evidence files and returns their URLs.")]
    [EndpointName("UploadCompensationEvidence")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> UploadCompensationEvidence(
        [FromForm] List<IFormFile> files,
        CancellationToken ct)
    {
        if (files is null || files.Count == 0)
        {
            return BadRequest("No files were uploaded.");
        }

        // Map IFormFile → UploadFileItem so the Application layer never sees ASP.NET types.
        var items = files
            .Where(f => f.Length > 0)
            .Select(f => new UploadFileItem(f.OpenReadStream(), f.FileName))
            .ToList();

        var result = await sender.Send(new UploadCompensationEvidenceCommand(items), ct);

        return result.Match(
            urls => Ok(new { urls }),
            Problem);
    }
}
