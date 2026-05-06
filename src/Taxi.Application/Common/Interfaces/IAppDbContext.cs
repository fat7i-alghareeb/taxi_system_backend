using Microsoft.EntityFrameworkCore;
using Taxi.Domain.Audit;
using Taxi.Domain.Auth;
using Taxi.Domain.Configuration;
using Taxi.Domain.Drivers;
using Taxi.Domain.Identity;
using Taxi.Domain.Notifications;
using Taxi.Domain.PaymentMethods;
using Taxi.Domain.Payments;
using Taxi.Domain.Promos;
using Taxi.Domain.Trips;
using Taxi.Domain.Users;
using Taxi.Domain.Vehicles;

namespace Taxi.Application.Common.Interfaces;

public interface IAppDbContext
{
    public DbSet<User> DomainUsers { get; }
    public DbSet<RefreshToken> RefreshTokens { get; }
    public DbSet<VehicleType> VehicleTypes { get; }
    public DbSet<Vehicle> Vehicles { get; }
    public DbSet<Driver> Drivers { get; }
    public DbSet<Trip> Trips { get; }
    public DbSet<Payment> Payments { get; }
    public DbSet<PromoCode> PromoCodes { get; }
    public DbSet<AuditLog> AuditLogs { get; }
    public DbSet<Notification> Notifications { get; }
    public DbSet<PassengerPaymentMethod> PaymentMethods { get; }
    public DbSet<AppConfig> AppConfigs { get; }
    public DbSet<OtpSession> OtpSessions { get; }
    public DbSet<TripRoute> TripRoutes { get; }
    public DbSet<PricingQuote> PricingQuotes { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
