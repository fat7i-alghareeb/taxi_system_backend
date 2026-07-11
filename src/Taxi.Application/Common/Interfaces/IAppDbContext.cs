using Microsoft.EntityFrameworkCore;
using Taxi.Domain.Admins;
using Taxi.Domain.Audit;
using Taxi.Domain.Auth;
using Taxi.Domain.Configuration;
using Taxi.Domain.CustomerIncidents;
using Taxi.Domain.Drivers;
using Taxi.Domain.Identity;
using Taxi.Domain.Invoices;
using Taxi.Domain.Notifications;
using Taxi.Domain.PaymentMethods;
using Taxi.Domain.Payments;
using Taxi.Domain.RefundIssues;
using Taxi.Domain.Trips;
using Taxi.Domain.Users;
using Taxi.Domain.Vehicles;
using Taxi.Domain.Wallet;

namespace Taxi.Application.Common.Interfaces;

public interface IAppDbContext
{
    public DbSet<AdminProfile> AdminProfiles { get; }
    public DbSet<User> DomainUsers { get; }
    public DbSet<RefreshToken> RefreshTokens { get; }
    public DbSet<VehicleType> VehicleTypes { get; }
    public DbSet<Driver> Drivers { get; }
    public DbSet<DriverDocument> DriverDocuments { get; }
    public DbSet<Trip> Trips { get; }
    public DbSet<TripMessage> TripMessages { get; }
    public DbSet<TripRecording> TripRecordings { get; }
    public DbSet<Payment> Payments { get; }
    public DbSet<PaymentRefund> PaymentRefunds { get; }
    public DbSet<AuditLog> AuditLogs { get; }
    public DbSet<Notification> Notifications { get; }
    public DbSet<PassengerPaymentMethod> PaymentMethods { get; }
    public DbSet<AppConfig> AppConfigs { get; }
    public DbSet<TripRoute> TripRoutes { get; }
    public DbSet<PricingQuote> PricingQuotes { get; }
    public DbSet<TripCancellation> TripCancellations { get; }
    public DbSet<PendingTripEdit> PendingTripEdits { get; }
    public DbSet<TripCompensationClaim> TripCompensationClaims { get; }
    public DbSet<TripWaitingSession> TripWaitingSessions { get; }
    public DbSet<Invoice> Invoices { get; }
    public DbSet<InvoiceCounter> InvoiceCounters { get; }
    public DbSet<CustomerIncident> CustomerIncidents { get; }
    public DbSet<RefundIssue> RefundIssues { get; }
    public DbSet<OtpCode> OtpCodes { get; }
    public DbSet<WalletAccount> WalletAccounts { get; }
    public DbSet<WalletTransaction> WalletTransactions { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}

