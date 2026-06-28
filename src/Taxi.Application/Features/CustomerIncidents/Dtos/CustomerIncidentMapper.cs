using Taxi.Domain.CustomerIncidents;

namespace Taxi.Application.Features.CustomerIncidents.Dtos;

public static class CustomerIncidentMapper
{
    public static CustomerIncidentDto ToDto(
        this CustomerIncident incident,
        string? passengerName = null,
        string? tripReferenceCode = null) =>
        new(
            incident.Id,
            incident.PassengerId,
            passengerName,
            incident.TripId,
            tripReferenceCode,
            incident.Type.ToString(),
            incident.Severity.ToString(),
            incident.Status.ToString(),
            incident.Title,
            incident.Reason,
            incident.Amount,
            incident.CurrencyCode,
            incident.CreatedAtUtc,
            incident.Notes,
            incident.ResolvedByAdminId,
            incident.ResolvedAtUtc);
}
