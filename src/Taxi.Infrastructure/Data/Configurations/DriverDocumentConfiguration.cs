using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Drivers;

namespace Taxi.Infrastructure.Data.Configurations;

public class DriverDocumentConfiguration : IEntityTypeConfiguration<DriverDocument>
{
    public void Configure(EntityTypeBuilder<DriverDocument> builder)
    {
        builder.HasKey(dd => dd.Id);

        builder.HasOne<Driver>()
            .WithMany()
            .HasForeignKey(dd => dd.DriverId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.Property(dd => dd.Type)
            .HasConversion<string>()
            .HasMaxLength(50)
            .IsRequired();

        builder.Property(dd => dd.FileUrl)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(dd => dd.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(dd => dd.ReviewNotes)
            .HasMaxLength(1000)
            .IsRequired(false);

        builder.Property(dd => dd.ReviewedAtUtc)
            .IsRequired(false);

        builder.Property(dd => dd.CreatedAtUtc)
            .IsRequired();

        builder.Property(dd => dd.LastModifiedUtc)
            .IsRequired(false);

        builder.Property(dd => dd.DeletedAtUtc)
            .IsRequired(false);

        builder.HasQueryFilter(dd => dd.DeletedAtUtc == null);
    }
}
