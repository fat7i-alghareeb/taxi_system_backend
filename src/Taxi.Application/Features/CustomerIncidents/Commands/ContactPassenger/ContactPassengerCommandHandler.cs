using MediatR;
using Microsoft.EntityFrameworkCore;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;
using Taxi.Domain.CustomerIncidents;

namespace Taxi.Application.Features.CustomerIncidents.Commands.ContactPassenger;

public sealed class ContactPassengerCommandHandler(
    IAppDbContext context,
    INotificationService notificationService)
    : IRequestHandler<ContactPassengerCommand, Result<Success>>
{
    public async Task<Result<Success>> Handle(ContactPassengerCommand request, CancellationToken ct)
    {
        var incident = await context.CustomerIncidents
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == request.IncidentId, ct);
        if (incident is null)
        {
            return CustomerIncidentErrors.NotFound;
        }

        // Free-text title/body: the FCM service leaves non-localization strings untouched.
        await notificationService.SendPushNotificationAsync(
            incident.PassengerId,
            request.Title.Trim(),
            request.Body.Trim(),
            new Dictionary<string, string>
            {
                { "type", "admin_message" },
                { "incidentId", incident.Id.ToString() },
            },
            ct);

        return Result.Success;
    }
}
