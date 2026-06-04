using FluentValidation;

using Taxi.Contracts.Common;

namespace Taxi.Application.Features.Audit.Queries.GetAuditLogs;

public class GetAuditLogsQueryValidator : AbstractValidator<GetAuditLogsQuery>
{
    public GetAuditLogsQueryValidator()
    {
        RuleFor(q => q.PageNumber)
            .GreaterThanOrEqualTo(1)
            .WithErrorCode(LocalizationKeys.Validation.PageInvalid);

        RuleFor(q => q.PageSize)
            .InclusiveBetween(1, 100)
            .WithErrorCode(LocalizationKeys.Validation.PageSizeInvalid);
    }
}
