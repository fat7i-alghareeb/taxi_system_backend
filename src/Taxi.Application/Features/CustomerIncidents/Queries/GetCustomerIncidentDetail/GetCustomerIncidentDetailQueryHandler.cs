using System.Text.Json;

using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.CustomerIncidents.Dtos;
using Taxi.Domain.Audit;
using Taxi.Domain.Common.Results;
using Taxi.Domain.CustomerIncidents;

namespace Taxi.Application.Features.CustomerIncidents.Queries.GetCustomerIncidentDetail;

/// <summary>
/// Returns the full incident view — including the sensitive material (recordings,
/// chat, locations) — and writes an audit-trail entry recording which admin viewed it.
/// </summary>
public sealed class GetCustomerIncidentDetailQueryHandler(IAppDbContext context, IUser currentUser)
    : IRequestHandler<GetCustomerIncidentDetailQuery, Result<CustomerIncidentDetailDto>>
{
    public async Task<Result<CustomerIncidentDetailDto>> Handle(GetCustomerIncidentDetailQuery request, CancellationToken ct)
    {
        var incident = await context.CustomerIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.IncidentId, ct);
        if (incident is null)
        {
            return CustomerIncidentErrors.NotFound;
        }

        var passenger = await context.DomainUsers
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == incident.PassengerId, ct);

        string? tripReferenceCode = null;
        string? tripStatus = null;
        var recordings = new List<IncidentRecordingDto>();
        var chatMessages = new List<IncidentChatMessageDto>();
        var locations = new List<IncidentLocationDto>();

        if (incident.TripId is { } tripId)
        {
            var trip = await context.Trips
                .AsNoTracking()
                .FirstOrDefaultAsync(t => t.Id == tripId, ct);
            if (trip is not null)
            {
                tripReferenceCode = trip.ReferenceCode;
                tripStatus = trip.Status.ToString();
                locations = trip.Stops
                    .OrderBy(s => s.Sequence)
                    .Select(s => new IncidentLocationDto(
                        (double)s.Coordinate.Latitude,
                        (double)s.Coordinate.Longitude,
                        s.AddressLabel,
                        s.Sequence))
                    .ToList();
            }

            recordings = await context.TripRecordings
                .AsNoTracking()
                .Where(r => r.TripId == tripId && r.DeletedAtUtc == null)
                .OrderBy(r => r.RecordedAtUtc)
                .Select(r => new IncidentRecordingDto(
                    r.Id,
                    r.FileUrl,
                    r.Type.ToString(),
                    r.DurationSeconds,
                    r.RecordedAtUtc))
                .ToListAsync(ct);

            // Chat may have been hard-deleted by TripChatCleanupService after the trip
            // ended; an empty list here simply means it is no longer retained.
            chatMessages = await context.TripMessages
                .AsNoTracking()
                .Where(m => m.TripId == tripId)
                .OrderBy(m => m.SentAtUtc)
                .Select(m => new IncidentChatMessageDto(
                    m.SenderRole.ToString(),
                    m.Content,
                    m.PhotoUrl,
                    m.SentAtUtc))
                .ToListAsync(ct);
        }

        await WriteAccessAuditAsync(incident, recordings.Count, chatMessages.Count, ct);

        var dto = new CustomerIncidentDetailDto(
            incident.ToDto(passenger?.Name, tripReferenceCode),
            passenger?.Phone,
            tripStatus,
            recordings,
            chatMessages,
            locations);

        return dto;
    }

    private async Task WriteAccessAuditAsync(
        CustomerIncident incident,
        int recordingCount,
        int chatCount,
        CancellationToken ct)
    {
        Guid? adminId = Guid.TryParse(currentUser.Id, out var id) ? id : null;
        var details = JsonSerializer.Serialize(new
        {
            incident.PassengerId,
            incident.TripId,
            recordingCount,
            chatCount,
        });

        var auditResult = AuditLog.Create(
            Guid.NewGuid(),
            adminId,
            action: "AccessedSensitiveData",
            entityName: nameof(CustomerIncident),
            entityId: incident.Id.ToString(),
            oldValue: null,
            newValue: details);
        if (auditResult.IsError)
        {
            return;
        }

        context.AuditLogs.Add(auditResult.Value);
        await context.SaveChangesAsync(ct);
    }
}
