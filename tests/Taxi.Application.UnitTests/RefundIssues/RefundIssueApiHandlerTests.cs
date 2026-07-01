using Microsoft.Extensions.Logging;
using NSubstitute;

using Taxi.Application.Common.Interfaces;
using Taxi.Application.Features.Payments.Dtos;
using Taxi.Application.Features.RefundIssues.Commands.SubmitRefundIssue;
using Taxi.Application.Features.RefundIssues.Dtos;
using Taxi.Application.Features.Refunds.Commands.RetryRefund;
using Taxi.Application.Features.Refunds.Queries.GetRefundById;
using Taxi.Application.Features.Refunds.Queries.GetRefunds;
using Taxi.Application.UnitTests.Infrastructure;
using Taxi.Contracts.Common;
using Taxi.Domain.Payments;
using Taxi.Domain.RefundIssues;
using Taxi.Domain.Trips;
using Taxi.Domain.Users;

using Xunit;

namespace Taxi.Application.UnitTests.RefundIssues;

public class RefundIssueApiHandlerTests
{
    [Fact]
    public async Task SubmitRefundIssue_CreatesSupportRecordAndLinksLatestRefund()
    {
        var passengerId = Guid.NewGuid();
        var trip = CreateTrip(passengerId);
        var payment = CreateCompletedPayment(trip.Id);
        var refund = CreateSucceededRefund(payment, 20m);
        var context = BuildContext(
            trips: [trip],
            payments: [payment],
            refunds: [refund],
            users: [PaymentTestBuilders.CreatePassenger(passengerId)]);
        var currentUser = Substitute.For<IUser>();
        currentUser.Id.Returns(passengerId.ToString());
        currentUser.IsAdmin.Returns(false);
        var notifications = Substitute.For<INotificationService>();
        var handler = new SubmitRefundIssueCommandHandler(
            context,
            currentUser,
            notifications,
            Substitute.For<ITripNotifier>(),
            Substitute.For<ILogger<SubmitRefundIssueCommandHandler>>());

        var result = await handler.Handle(
            new SubmitRefundIssueCommand(
                trip.Id,
                nameof(RefundIssueRequestType.DidNotReceiveRefund),
                "did-not-receive",
                "Bank account still shows no refund.",
                false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(refund.Id, result.Value.PaymentRefundId);
        Assert.Equal(PaymentRefundStatus.Succeeded.ToString(), result.Value.RefundStatusSnapshot);
        Assert.Equal(refund.Amount, result.Value.RefundAmountSnapshot);
        Assert.Equal(RefundIssueReviewStatus.Open.ToString(), result.Value.ReviewStatus);
        context.RefundIssues.Received(1).Add(Arg.Is<RefundIssue>(issue =>
            issue.TripId == trip.Id &&
            issue.PaymentId == payment.Id &&
            issue.PaymentRefundId == refund.Id));
        await notifications.Received(1).SendPushNotificationToAdminsAsync(
            LocalizationKeys.RefundIssue.CreatedAdminTitle,
            LocalizationKeys.RefundIssue.CreatedAdminBody,
            Arg.Any<Dictionary<string, string>>(),
            Arg.Any<CancellationToken>(),
            Arg.Is<object[]?>(args => args == null),
            Arg.Is<object[]?>(args => args != null && args.Length == 1));
    }

    [Fact]
    public async Task SubmitRefundIssue_WhenPaymentDoesNotExist_CreatesSupportRecordFromCancellationSnapshot()
    {
        var passengerId = Guid.NewGuid();
        var trip = CreateTrip(passengerId);
        var cancellation = CreateCancellation(trip.Id, 20m, 20m);
        var context = BuildContext(
            trips: [trip],
            cancellations: [cancellation],
            users: [PaymentTestBuilders.CreatePassenger(passengerId)]);
        var currentUser = Substitute.For<IUser>();
        currentUser.Id.Returns(passengerId.ToString());
        currentUser.IsAdmin.Returns(false);
        var handler = new SubmitRefundIssueCommandHandler(
            context,
            currentUser,
            Substitute.For<INotificationService>(),
            Substitute.For<ITripNotifier>(),
            Substitute.For<ILogger<SubmitRefundIssueCommandHandler>>());

        var result = await handler.Handle(
            new SubmitRefundIssueCommand(
                trip.Id,
                nameof(RefundIssueRequestType.ReceivedLessThanExpected),
                "received-less",
                "No card payment exists because Stripe is disabled.",
                false),
            CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Null(result.Value.PaymentId);
        Assert.Null(result.Value.PaymentRefundId);
        Assert.Equal(cancellation.Id, result.Value.TripCancellationId);
        Assert.Equal(cancellation.RefundAmount, result.Value.RefundAmountSnapshot);
        Assert.Equal(cancellation.CurrencyCode, result.Value.RefundCurrencySnapshot);
        context.RefundIssues.Received(1).Add(Arg.Is<RefundIssue>(issue =>
            issue.PaymentId == null &&
            issue.TripCancellationId == cancellation.Id &&
            issue.RefundAmountSnapshot == cancellation.RefundAmount));
    }

    [Fact]
    public void CustomerRefundDtos_DoNotExposeStripeInternals()
    {
        var customerRefundProperties = typeof(RefundSummaryDto)
            .GetProperties()
            .Select(property => property.Name)
            .ToList();
        var refundIssueProperties = typeof(RefundIssueDto)
            .GetProperties()
            .Select(property => property.Name)
            .ToList();

        Assert.DoesNotContain(customerRefundProperties, name => name.Contains("Stripe", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refundIssueProperties, name => name.Contains("Stripe", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(customerRefundProperties, name => name.Contains("FailureReason", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(refundIssueProperties, name => name.Contains("FailureReason", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task GetRefundById_ReturnsAdminStripeAndFailureDetails()
    {
        var passengerId = Guid.NewGuid();
        var trip = CreateTrip(passengerId);
        var payment = CreateCompletedPayment(trip.Id);
        var refund = CreateFailedRefund(payment, 35m);
        var context = BuildContext(trips: [trip], payments: [payment], refunds: [refund]);
        var handler = new GetRefundByIdQueryHandler(context);

        var result = await handler.Handle(new GetRefundByIdQuery(refund.Id), CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal(refund.StripeRefundId, result.Value.StripeRefundId);
        Assert.Equal(payment.StripePaymentIntentId, result.Value.StripePaymentIntentId);
        Assert.Equal(payment.StripeChargeId, result.Value.StripeChargeId);
        Assert.Equal("stripe_failed", result.Value.FailureCode);
        Assert.Equal("Stripe declined the refund.", result.Value.FailureReason);
        Assert.True(result.Value.CanRetry);
    }

    [Fact]
    public async Task GetRefunds_ReturnsFailedCancellationWorkItemWhenNoPaymentRefundExists()
    {
        var passengerId = Guid.NewGuid();
        var trip = CreateTrip(passengerId);
        var cancellation = CreateCancellation(trip.Id, 25m, 25m);
        var context = BuildContext(trips: [trip], cancellations: [cancellation]);
        var handler = new GetRefundsQueryHandler(context);

        var result = await handler.Handle(new GetRefundsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Null(item.RefundId);
        Assert.Null(item.PaymentId);
        Assert.Equal(cancellation.Id, item.TripCancellationId);
        Assert.Equal(PaymentRefundStatus.Failed.ToString(), item.Status);
        Assert.Equal(PaymentRefundSourceType.PassengerCancellation.ToString(), item.SourceType);
        Assert.Equal(cancellation.RefundAmount, item.Amount);
        Assert.True(item.RequiresAdminAction);
        Assert.False(item.IsManualObligation);
        Assert.False(item.CanRetry);
        Assert.Equal(PaymentErrors.RefundUnavailable.Code, item.FailureCode);
        Assert.Equal(PaymentErrors.RefundUnavailable.Code, item.RetryBlockedReason);
    }

    [Fact]
    public async Task GetRefunds_DoesNotDuplicateCancellationWhenPaymentRefundExists()
    {
        var passengerId = Guid.NewGuid();
        var trip = CreateTrip(passengerId);
        var payment = CreateCompletedPayment(trip.Id);
        var cancellation = CreateCancellation(trip.Id, 25m, 25m);
        var refund = CreateSucceededRefund(payment, 25m, cancellation.Id);
        var context = BuildContext(
            trips: [trip],
            payments: [payment],
            refunds: [refund],
            cancellations: [cancellation]);
        var handler = new GetRefundsQueryHandler(context);

        var result = await handler.Handle(new GetRefundsQuery(), CancellationToken.None);

        Assert.True(result.IsSuccess);
        var item = Assert.Single(result.Value.Items);
        Assert.Equal(refund.Id, item.RefundId);
        Assert.False(item.IsManualObligation);
        Assert.Equal(cancellation.Id, item.TripCancellationId);
    }

    [Fact]
    public async Task RetryRefund_AlwaysDelegatesEligibilityToLifecycleService()
    {
        var passengerId = Guid.NewGuid();
        var adminId = Guid.NewGuid();
        var trip = CreateTrip(passengerId);
        var payment = CreateCompletedPayment(trip.Id);
        var refund = CreateFailedRefund(payment, 35m);
        var context = BuildContext(trips: [trip], payments: [payment], refunds: [refund]);
        var currentUser = Substitute.For<IUser>();
        currentUser.Id.Returns(adminId.ToString());
        currentUser.IsAdmin.Returns(true);
        var lifecycle = Substitute.For<IRefundLifecycleService>();
        lifecycle.RetryRefundAsync(refund.Id, adminId, "retry note", Arg.Any<CancellationToken>())
            .Returns(PaymentErrors.RefundFullyRefunded);
        var handler = new RetryRefundCommandHandler(
            context,
            currentUser,
            lifecycle,
            Substitute.For<INotificationService>(),
            Substitute.For<ILogger<RetryRefundCommandHandler>>());

        var result = await handler.Handle(new RetryRefundCommand(refund.Id, "retry note"), CancellationToken.None);

        Assert.True(result.IsFailure);
        Assert.Equal(PaymentErrors.RefundFullyRefunded.Code, result.Error.Code);
        await lifecycle.Received(1).RetryRefundAsync(refund.Id, adminId, "retry note", Arg.Any<CancellationToken>());
    }

    private static Trip CreateTrip(Guid passengerId)
    {
        var quote = PaymentTestBuilders.CreateValidQuote(passengerId, Guid.NewGuid());
        var stops = new[]
        {
            TripStop.Create(new Taxi.Domain.Trips.Coordinate(52.37m, 4.89m), 0, "From").Value,
            TripStop.Create(new Taxi.Domain.Trips.Coordinate(52.38m, 4.90m), 1, "To").Value,
        };

        return Trip.Request(
            Guid.NewGuid(),
            "TRP-RFND1",
            passengerId,
            quote,
            stops,
            scheduledAtUtc: null).Value;
    }

    private static Payment CreateCompletedPayment(Guid tripId, decimal amount = 100m)
    {
        var payment = Payment.CreateForStripe(
            Guid.NewGuid(),
            tripId,
            amount,
            "eur",
            "pi_test_123",
            "cs_test_secret").Value;
        payment.MarkAsCompleted("ch_test_123", "card");
        return payment;
    }

    private static PaymentRefund CreateSucceededRefund(Payment payment, decimal amount, Guid? tripCancellationId = null)
    {
        var refund = PaymentRefund.Create(
            Guid.NewGuid(),
            payment.Id,
            PaymentRefundSourceType.PassengerCancellation,
            amount,
            payment.Currency,
            payment.Amount,
            refundPercent: amount,
            tripId: payment.TripId,
            tripCancellationId: tripCancellationId,
            stripePaymentIntentId: payment.StripePaymentIntentId,
            stripeChargeId: payment.StripeChargeId,
            idempotencyKey: Guid.NewGuid().ToString("N")).Value;
        refund.MarkAttemptStarted(refund.IdempotencyKey!);
        refund.MarkPending("re_test_123", payment.StripePaymentIntentId, payment.StripeChargeId);
        refund.MarkSucceeded();
        return refund;
    }

    private static TripCancellation CreateCancellation(Guid tripId, decimal refundPercent, decimal refundAmount)
        => TripCancellation.Create(
            Guid.NewGuid(),
            tripId,
            CancellationActor.Passenger,
            CancellationReason.PassengerAfterOneHour,
            refundPercent,
            refundAmount,
            "EUR",
            "customer cancelled").Value;

    private static PaymentRefund CreateFailedRefund(Payment payment, decimal amount)
    {
        var refund = PaymentRefund.Create(
            Guid.NewGuid(),
            payment.Id,
            PaymentRefundSourceType.ManualIncidentRefund,
            amount,
            payment.Currency,
            payment.Amount,
            tripId: payment.TripId,
            customerIncidentId: Guid.NewGuid(),
            stripePaymentIntentId: payment.StripePaymentIntentId,
            stripeChargeId: payment.StripeChargeId,
            idempotencyKey: Guid.NewGuid().ToString("N")).Value;
        refund.MarkAttemptStarted(refund.IdempotencyKey!);
        refund.MarkPending("re_test_failed", payment.StripePaymentIntentId, payment.StripeChargeId);
        refund.MarkFailed("stripe_failed", "Stripe declined the refund.", canRetry: true);
        return refund;
    }

    private static IAppDbContext BuildContext(
        List<Trip>? trips = null,
        List<Payment>? payments = null,
        List<PaymentRefund>? refunds = null,
        List<User>? users = null,
        List<RefundIssue>? issues = null,
        List<TripCancellation>? cancellations = null)
    {
        var tripsSet = DbSetMockFactory.Create(trips ?? []);
        var paymentsSet = DbSetMockFactory.Create(payments ?? []);
        var refundsSet = DbSetMockFactory.Create(refunds ?? []);
        var usersSet = DbSetMockFactory.Create(users ?? []);
        var issuesSet = DbSetMockFactory.Create(issues ?? []);
        var cancellationsSet = DbSetMockFactory.Create(cancellations ?? []);
        var context = Substitute.For<IAppDbContext>();
        context.Trips.Returns(tripsSet);
        context.Payments.Returns(paymentsSet);
        context.PaymentRefunds.Returns(refundsSet);
        context.DomainUsers.Returns(usersSet);
        context.RefundIssues.Returns(issuesSet);
        context.TripCancellations.Returns(cancellationsSet);
        context.SaveChangesAsync(Arg.Any<CancellationToken>()).Returns(1);
        return context;
    }
}
