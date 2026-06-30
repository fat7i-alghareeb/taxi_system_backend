using FluentValidation;
using Taxi.Contracts.Common;

namespace Taxi.Application.Features.RefundIssues.Commands.ReviewRefundIssue;

public sealed class ReviewRefundIssueCommandValidator : AbstractValidator<ReviewRefundIssueCommand>
{
    public ReviewRefundIssueCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage(LocalizationKeys.RefundIssue.NotFound);

        RuleFor(x => x.ReviewStatus)
            .NotEmpty()
            .WithMessage(LocalizationKeys.RefundIssue.InvalidReviewStatus);

        RuleFor(x => x.AdminNotes)
            .MaximumLength(2000)
            .WithMessage(LocalizationKeys.Validation.InvalidFormat);
    }
}
