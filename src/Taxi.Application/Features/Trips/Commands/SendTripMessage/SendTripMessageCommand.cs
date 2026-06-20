using MediatR;

using Taxi.Application.Features.Trips.Dtos;
using Taxi.Application.Features.Uploads.Commands.UploadCompensationEvidence;
using Taxi.Domain.Common.Results;

namespace Taxi.Application.Features.Trips.Commands.SendTripMessage;

/// <summary>
/// Sends a chat message (text and/or a single photo) on a live trip. The sender
/// is resolved from the current user; <see cref="Photo"/> is null for text-only
/// messages. Reuses <see cref="UploadFileItem"/> so the Application layer stays
/// free of ASP.NET types.
/// </summary>
public record SendTripMessageCommand(
    Guid TripId,
    string? Content,
    UploadFileItem? Photo)
    : IRequest<Result<TripMessageDto>>;
