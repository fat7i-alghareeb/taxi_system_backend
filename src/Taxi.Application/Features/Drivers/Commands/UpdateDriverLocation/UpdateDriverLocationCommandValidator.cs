using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Drivers.Commands.UpdateDriverLocation;

public class UpdateDriverLocationCommandValidator : AbstractValidator<UpdateDriverLocationCommand>
{
    public UpdateDriverLocationCommandValidator()
    {
        RuleFor(c => c.Latitude)
            .InclusiveBetween(-90.0, 90.0)
            .WithErrorCode(LocalizationKeys.Trip.InvalidCoordinate);

        RuleFor(c => c.Longitude)
            .InclusiveBetween(-180.0, 180.0)
            .WithErrorCode(LocalizationKeys.Trip.InvalidCoordinate);
    }
}
