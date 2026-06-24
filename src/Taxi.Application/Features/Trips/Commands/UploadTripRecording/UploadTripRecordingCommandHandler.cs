using MediatR;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Application.Common.Uploads;
using Taxi.Contracts.Common;
using Taxi.Domain.Common.Results;
using Taxi.Domain.Trips;

namespace Taxi.Application.Features.Trips.Commands.UploadTripRecording;

public class UploadTripRecordingCommandHandler(
    IAppDbContext context,
    IFileStorage fileStorage,
    IUser currentUser)
    : IRequestHandler<UploadTripRecordingCommand, Result<string>>
{
    private readonly IAppDbContext _context = context;
    private readonly IFileStorage _fileStorage = fileStorage;
    private readonly IUser _currentUser = currentUser;

    public async Task<Result<string>> Handle(UploadTripRecordingCommand request, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(_currentUser.Id) || !Guid.TryParse(_currentUser.Id, out var passengerId))
        {
            return Error.Unauthorized(LocalizationKeys.Auth.UserIdClaimInvalid, "Invalid user ID claim.");
        }

        var trip = await _context.Trips
            .FirstOrDefaultAsync(t => t.Id == request.TripId, ct);
        if (trip is null)
        {
            return TripErrors.NotFound;
        }

        // Only the passenger who owns the trip may attach a safety recording to it.
        if (trip.PassengerId != passengerId)
        {
            return TripErrors.NotOwnedByPassenger;
        }

        var validation = AudioUploadPolicy.Validate(request.File.Stream, request.File.FileName);
        if (validation.IsFailure)
        {
            return validation.Error;
        }

        var extension = Path.GetExtension(request.File.FileName);
        var url = await _fileStorage.SaveAsync(
            request.File.Stream,
            $"recordings/{trip.Id:N}/{Guid.NewGuid():N}{extension}",
            ct);

        var sizeBytes = request.File.Stream.CanSeek ? request.File.Stream.Length : 0L;

        var recording = TripRecording.Create(
            trip.Id,
            passengerId,
            RecordingType.Audio,
            url,
            sizeBytes,
            request.DurationSeconds);

        _context.TripRecordings.Add(recording);
        await _context.SaveChangesAsync(ct);

        return url;
    }
}
