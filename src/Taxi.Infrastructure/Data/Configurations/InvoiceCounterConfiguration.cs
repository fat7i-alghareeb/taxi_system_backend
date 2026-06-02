using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.Invoices;

namespace Taxi.Infrastructure.Data.Configurations;

public class InvoiceCounterConfiguration : IEntityTypeConfiguration<InvoiceCounter>
{
    public void Configure(EntityTypeBuilder<InvoiceCounter> builder)
    {
        builder.HasKey(x => x.YearMonth);

        builder.Property(x => x.YearMonth)
            .IsRequired()
            .HasMaxLength(6)
            .IsFixedLength();

        builder.Property(x => x.NextSequence).IsRequired();
    }
}
