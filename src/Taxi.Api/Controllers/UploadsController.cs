using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Common.Interfaces;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/uploads")]
public class UploadsController(IFileStorage fileStorage) : ApiController
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

        var urls = new List<string>();
        foreach (var file in files)
        {
            if (file.Length == 0)
            {
                continue;
            }

            var extension = Path.GetExtension(file.FileName);
            await using var stream = file.OpenReadStream();
            var url = await fileStorage.SaveAsync(
                stream,
                $"compensation/{Guid.NewGuid():N}{extension}",
                ct);
            urls.Add(url);
        }

        return Ok(new { urls });
    }
}
