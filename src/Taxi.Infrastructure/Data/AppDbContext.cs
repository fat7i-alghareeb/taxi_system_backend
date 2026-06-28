using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Admins;
using Taxi.Domain.Audit;
using Taxi.Domain.Common;
using Taxi.Domain.Configuration;
using Taxi.Domain.CustomerIncidents;
using Taxi.Domain.Drivers;
using Taxi.Domain.Identity;
using Taxi.Domain.Invoices;
using Taxi.Domain.Notifications;
using Taxi.Domain.PaymentMethods;
using Taxi.Domain.Payments;
using Taxi.Domain.Trips;
using Taxi.Domain.Users;
using Taxi.Domain.Vehicles;
using Taxi.Infrastructure.Identity;
using Taxi.Infrastructure.Outbox;

namespace Taxi.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : IdentityDbContext<AppUser>(options), IAppDbContext
{
    public DbSet<AdminProfile> AdminProfiles => this.Set<AdminProfile>();
    public DbSet<RefreshToken> RefreshTokens => this.Set<RefreshToken>();
    public DbSet<User> DomainUsers => this.Set<User>();
    public DbSet<VehicleType> VehicleTypes => this.Set<VehicleType>();
    public DbSet<Driver> Drivers => this.Set<Driver>();
    public DbSet<DriverDocument> DriverDocuments => this.Set<DriverDocument>();
    public DbSet<Trip> Trips => this.Set<Trip>();
    public DbSet<TripMessage> TripMessages => this.Set<TripMessage>();
    public DbSet<TripRecording> TripRecordings => this.Set<TripRecording>();
    public DbSet<Payment> Payments => this.Set<Payment>();
    public DbSet<AuditLog> AuditLogs => this.Set<AuditLog>();
    public DbSet<Notification> Notifications => this.Set<Notification>();
    public DbSet<PassengerPaymentMethod> PaymentMethods => this.Set<PassengerPaymentMethod>();
    public DbSet<AppConfig> AppConfigs => this.Set<AppConfig>();
    public DbSet<TripRoute> TripRoutes => this.Set<TripRoute>();
    public DbSet<PricingQuote> PricingQuotes => this.Set<PricingQuote>();
    public DbSet<TripCancellation> TripCancellations => this.Set<TripCancellation>();
    public DbSet<TripCompensationClaim> TripCompensationClaims => this.Set<TripCompensationClaim>();
    public DbSet<TripWaitingSession> TripWaitingSessions => this.Set<TripWaitingSession>();
    public DbSet<Invoice> Invoices => this.Set<Invoice>();
    public DbSet<InvoiceCounter> InvoiceCounters => this.Set<InvoiceCounter>();
    public DbSet<CustomerIncident> CustomerIncidents => this.Set<CustomerIncident>();

    // Transactional outbox table. Rows are written automatically by
    // ConvertDomainEventsToOutboxInterceptor on save, and drained by OutboxDispatcherService.
    public DbSet<OutboxMessage> OutboxMessages => this.Set<OutboxMessage>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
    }
}

