using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Drivers.Queries.GetDriverDocuments;

public class GetDriverDocumentsQueryValidator : AbstractValidator<GetDriverDocumentsQuery>
{
    public GetDriverDocumentsQueryValidator()
    {
        RuleFor(q => q.DriverId)
            .NotEmpty().WithErrorCode(LocalizationKeys.DriverDocument.DriverIdRequired);
    }
}
