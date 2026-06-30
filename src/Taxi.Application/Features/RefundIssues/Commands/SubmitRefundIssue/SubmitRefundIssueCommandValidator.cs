using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.RefundIssues.Commands.SubmitRefundIssue;

public sealed class SubmitRefundIssueCommandValidator : AbstractValidator<SubmitRefundIssueCommand>
{
    public SubmitRefundIssueCommandValidator()
    {
        RuleFor(x => x.TripId)
            .NotEmpty()
            .WithMessage(LocalizationKeys.Trip.NotFound);

        RuleFor(x => x.RequestType)
            .NotEmpty()
            .WithMessage(LocalizationKeys.RefundIssue.InvalidRequestType);

        RuleFor(x => x.CustomerReason)
            .NotEmpty()
            .WithMessage(LocalizationKeys.Validation.RequiredField)
            .MaximumLength(120)
            .WithMessage(LocalizationKeys.Validation.InvalidFormat);

        RuleFor(x => x.Note)
            .MaximumLength(2000)
            .WithMessage(LocalizationKeys.Validation.InvalidFormat);
    }
}
