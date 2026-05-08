using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Taxi.Domain.PaymentMethods;

namespace Taxi.Infrastructure.Data.Configurations;

public class PassengerPaymentMethodConfiguration : IEntityTypeConfiguration<PassengerPaymentMethod>
{
    public void Configure(EntityTypeBuilder<PassengerPaymentMethod> builder)
    {
        builder.HasKey(m => m.Id);

        builder.Property(m => m.GatewayPaymentMethodId)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(m => m.CardBrand)
            .HasMaxLength(50);

        builder.Property(m => m.LastFour)
            .HasMaxLength(4)
            .IsFixedLength();
    }
}

