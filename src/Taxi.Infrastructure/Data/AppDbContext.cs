using MediatR;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Audit;
using Taxi.Domain.Auth;
using Taxi.Domain.Common;
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
using Taxi.Infrastructure.Identity;

namespace Taxi.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options, IMediator mediator) : IdentityDbContext<AppUser>(options), IAppDbContext
{
    public DbSet<RefreshToken> RefreshTokens => this.Set<RefreshToken>();
    public DbSet<User> DomainUsers => this.Set<User>();
    public DbSet<VehicleType> VehicleTypes => this.Set<VehicleType>();
    public DbSet<Vehicle> Vehicles => this.Set<Vehicle>();
    public DbSet<Driver> Drivers => this.Set<Driver>();
    public DbSet<Trip> Trips => this.Set<Trip>();
    public DbSet<Payment> Payments => this.Set<Payment>();
    public DbSet<PromoCode> PromoCodes => this.Set<PromoCode>();
    public DbSet<AuditLog> AuditLogs => this.Set<AuditLog>();
    public DbSet<Notification> Notifications => this.Set<Notification>();
    public DbSet<PassengerPaymentMethod> PaymentMethods => this.Set<PassengerPaymentMethod>();
    public DbSet<AppConfig> AppConfigs => this.Set<AppConfig>();
    public DbSet<OtpSession> OtpSessions => this.Set<OtpSession>();
    public DbSet<TripRoute> TripRoutes => this.Set<TripRoute>();
    public DbSet<PricingQuote> PricingQuotes => this.Set<PricingQuote>();

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        await this.DispatchDomainEventsAsync(cancellationToken);
        return await base.SaveChangesAsync(cancellationToken);
    }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }

    private async Task DispatchDomainEventsAsync(CancellationToken cancellationToken)
    {
        var domainEntities = this.ChangeTracker.Entries()
            .Where(e => e.Entity is Entity baseEntity && baseEntity.DomainEvents.Count != 0)
            .Select(e => (Entity)e.Entity)
            .ToList();

        var domainEvents = domainEntities
            .SelectMany(e => e.DomainEvents)
            .ToList();

        foreach (var domainEvent in domainEvents)
        {
            await mediator.Publish(domainEvent, cancellationToken);
        }

        foreach (var entity in domainEntities)
        {
            entity.ClearDomainEvents();
        }
    }
}

