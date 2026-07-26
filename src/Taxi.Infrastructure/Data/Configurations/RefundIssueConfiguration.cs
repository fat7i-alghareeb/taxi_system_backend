using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using Taxi.Domain.Payments;
using Taxi.Domain.RefundIssues;
using Taxi.Domain.Trips;
using Taxi.Domain.Users;

namespace Taxi.Infrastructure.Data.Configurations;

public sealed class RefundIssueConfiguration : IEntityTypeConfiguration<RefundIssue>
{
    public void Configure(EntityTypeBuilder<RefundIssue> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.RequestType)
            .HasConversion<string>()
            .HasMaxLength(60)
            .IsRequired();

        builder.Property(x => x.ReviewStatus)
            .HasConversion<string>()
            .HasMaxLength(30)
            .IsRequired();

        builder.Property(x => x.CustomerReason)
            .HasMaxLength(120)
            .IsRequired();

        builder.Property(x => x.Note)
            .HasMaxLength(2000);

        builder.Property(x => x.RefundStatusSnapshot)
            .HasConversion<string>()
            .HasMaxLength(40);

        builder.Property(x => x.RefundAmountSnapshot)
            .HasPrecision(18, 2);

        builder.Property(x => x.RefundCurrencySnapshot)
            .HasMaxLength(3);

        builder.Property(x => x.AdminNotes)
            .HasColumnType("text");

        builder.Property(x => x.CreatedAtUtc)
            .IsRequired();

        builder.Property(x => x.LastModifiedUtc)
            .IsRequired(false);

        builder.HasIndex(x => x.PassengerId);
        builder.HasIndex(x => x.TripId);
        builder.HasIndex(x => x.PaymentId);
        builder.HasIndex(x => x.PaymentRefundId);
        builder.HasIndex(x => x.TripCancellationId);
        builder.HasIndex(x => new { x.ReviewStatus, x.CreatedAtUtc });

        // At most ONE Open/InReview refund review per passenger per trip. The handler check in
        // SubmitRefundIssueCommandHandler is TOCTOU on its own; this is what actually holds the
        // invariant against retried or replayed requests.
        //
        // ReviewStatus is persisted as text (HasConversion<string>() above), so the partial-index
        // predicate compares string literals, not enum ordinals. The non-unique HasIndex(TripId)
        // above stays: the admin list's ?tripId= filter needs it and a partial index cannot serve
        // unfiltered lookups.
        builder.HasIndex(x => new { x.TripId, x.PassengerId })
            .IsUnique()
            .HasFilter("\"ReviewStatus\" IN ('Open', 'InReview')")
            .HasDatabaseName("IX_RefundIssues_TripId_PassengerId_Open");

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.PassengerId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Trip>()
            .WithMany()
            .HasForeignKey(x => x.TripId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<Payment>()
            .WithMany()
            .HasForeignKey(x => x.PaymentId)
            .IsRequired(false)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<PaymentRefund>()
            .WithMany()
            .HasForeignKey(x => x.PaymentRefundId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<TripCancellation>()
            .WithMany()
            .HasForeignKey(x => x.TripCancellationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}