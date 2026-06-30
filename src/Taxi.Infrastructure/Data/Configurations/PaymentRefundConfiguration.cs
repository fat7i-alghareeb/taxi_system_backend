using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Admins;
using Taxi.Domain.CustomerIncidents;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;
using Taxi.Domain.Users;

namespace Taxi.Infrastructure.Data.Configurations;

public sealed class PaymentRefundConfiguration : IEntityTypeConfiguration<PaymentRefund>
{
    public void Configure(EntityTypeBuilder<PaymentRefund> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.SourceType)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<string>()
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(x => x.Amount)
            .HasPrecision(18, 2);

        builder.Property(x => x.OriginalPaymentAmountSnapshot)
            .HasPrecision(18, 2);

        builder.Property(x => x.Currency)
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(x => x.RefundPercent)
            .HasPrecision(5, 2);

        builder.Property(x => x.StripeRefundId)
            .HasMaxLength(255);

        builder.Property(x => x.StripePaymentIntentId)
            .HasMaxLength(255);

        builder.Property(x => x.StripeChargeId)
            .HasMaxLength(255);

        builder.Property(x => x.IdempotencyKey)
            .HasMaxLength(255);

        builder.Property(x => x.FailureCode)
            .HasMaxLength(100);

        builder.Property(x => x.FailureReason)
            .HasMaxLength(2000);

        builder.Property(x => x.SafeCustomerFailureMessage)
            .HasMaxLength(500);

        builder.Property(x => x.RetryBlockedReason)
            .HasMaxLength(500);

        builder.Property(x => x.LastStripeEventId)
            .HasMaxLength(255);

        builder.Property(x => x.AdminNote)
            .HasMaxLength(2000);

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.LastModifiedUtc)
            .IsRequired(false);

        builder.HasIndex(x => x.PaymentId);
        builder.HasIndex(x => x.TripId);
        builder.HasIndex(x => x.CustomerIncidentId);
        builder.HasIndex(x => x.TripCancellationId);
        builder.HasIndex(x => x.TripCompensationClaimId);
        builder.HasIndex(x => x.Status);
        builder.HasIndex(x => x.RequiresAdminAction);

        builder.HasIndex(x => x.StripeRefundId)
            .IsUnique()
            .HasFilter("\"StripeRefundId\" IS NOT NULL");

        builder.HasIndex(x => x.IdempotencyKey)
            .IsUnique();

        builder.HasOne<Payment>()
            .WithMany()
            .HasForeignKey(x => x.PaymentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TripCancellation>()
            .WithMany()
            .HasForeignKey(x => x.TripCancellationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CustomerIncident>()
            .WithMany()
            .HasForeignKey(x => x.CustomerIncidentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TripCompensationClaim>()
            .WithMany()
            .HasForeignKey(x => x.TripCompensationClaimId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<AdminProfile>()
            .WithMany()
            .HasForeignKey(x => x.RequestedByAdminId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
