using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Drivers.Commands.ReviewDriverDocument;
using Taxi.Application.Features.Drivers.Commands.UploadDriverDocument;
using Taxi.Application.Features.Drivers.Queries.GetDriverDocuments;
using Taxi.Domain.Drivers;

namespace Taxi.Api.Controllers;

[ApiVersion("1.0")]
[Route("api/v{version:apiVersion}/drivers/{id:guid}/documents")]
public class DriverDocumentsController(ISender sender, IFileStorage fileStorage) : ApiController
{
    [HttpPost]
    [Authorize(Roles = "Admin,Driver")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [EndpointSummary("Uploads a KYC document for a driver.")]
    [EndpointName("UploadDriverDocument")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Upload(Guid id, [FromForm] DocumentType type, IFormFile file, CancellationToken ct)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest("No file was uploaded.");
        }

        var extension = Path.GetExtension(file.FileName);
        using var stream = file.OpenReadStream();
        
        // Save using IFileStorage
        var fileUrl = await fileStorage.SaveAsync(stream, $"documents/{id}_{type}{extension}", ct);

        var result = await sender.Send(new UploadDriverDocumentCommand(id, type, fileUrl), ct);

        return result.Match(
            _ => Ok(new { FileUrl = fileUrl }),
            Problem);
    }

    [HttpGet]
    [Authorize(Roles = "Admin,Driver")]
    [ProducesResponseType(typeof(List<DriverDocumentDto>), StatusCodes.Status200OK)]
    [EndpointSummary("Retrieves all KYC documents for a driver.")]
    [EndpointName("GetDriverDocuments")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> GetDocuments(Guid id, CancellationToken ct)
    {
        var result = await sender.Send(new GetDriverDocumentsQuery(id), ct);

        return result.Match(
            Ok,
            Problem);
    }

    [HttpPut("{docId:guid}/review")]
    [Authorize(Roles = "Admin")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [EndpointSummary("Admin reviews and approves/rejects a driver document.")]
    [EndpointName("ReviewDriverDocument")]
    [MapToApiVersion("1.0")]
    public async Task<IActionResult> Review(Guid id, Guid docId, [FromBody] ReviewDocumentRequest request, CancellationToken ct)
    {
        var result = await sender.Send(new ReviewDriverDocumentCommand(id, docId, request.Approved, request.Notes), ct);

        return result.Match(
            _ => NoContent(),
            Problem);
    }
}

public record ReviewDocumentRequest(bool Approved, string? Notes);
