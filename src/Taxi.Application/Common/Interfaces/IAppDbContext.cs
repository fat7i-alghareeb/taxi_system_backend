using Microsoft.EntityFrameworkCore;

using Taxi.Domain.Drivers;
using Taxi.Domain.Identity;
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

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}