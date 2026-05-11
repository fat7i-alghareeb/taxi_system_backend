using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

using Taxi.Domain.Configuration;
using Taxi.Domain.Drivers;
using Taxi.Domain.Users;
using Taxi.Domain.Vehicles;
using Taxi.Infrastructure.Identity;

namespace Taxi.Infrastructure.Data;

public class ApplicationDbContextInitialiser(
    AppDbContext context,
    RoleManager<IdentityRole> roleManager,
    UserManager<AppUser> userManager,
    IHostEnvironment environment)
{
    private const string SuperAdminPhone = "+963900000000";
    private const string SuperAdminPassword = "Admin@123!";
    private static readonly Guid SuperAdminUserId = new("00000000-0000-0000-0000-000000000000");
    private static readonly Guid AdminDriverUserId = new("11111111-1111-1111-1111-111111111111");
    private static readonly Guid AdminDriverId = new("22222222-2222-2222-2222-222222222222");

    public async Task InitialiseAsync()
    {
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
    }

    public async Task SeedAsync()
    {
        try
        {
            await TrySeedAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public async Task TrySeedAsync()
    {
        // 1. Seed Roles
        var roles = new[] { "Admin", "Driver", "Passenger" };
        foreach (var roleName in roles)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(roleName));
                if (!result.Succeeded)
                {
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    throw new InvalidOperationException($"Failed to create role '{roleName}': {errors}");
                }
            }
        }

        // 2. Seed Vehicle Types
        var existingCodes = await context.VehicleTypes
            .Select(v => v.Code)
            .ToListAsync();

        var existingCodeSet = new HashSet<string>(existingCodes);
        var addedAny = false;

        var types = new List<VehicleType>
        {
            VehicleType.Create(Guid.NewGuid(), "standard", "Standard", "عادي", "Standaard", 4, 2.8m, 0.20m, 5.0m).Value,
            VehicleType.Create(Guid.NewGuid(), "xl", "XL Van", "فان كبير", "XL Van", 8, 3.20m, 0.25m, 8.0m).Value,
            VehicleType.Create(Guid.NewGuid(), "wheelchair", "Wheelchair Taxi", "تاكسي ذوي الاحتياجات الخاصة", "Rolstoel Taxi", 4, 3.50m, 0.25m, 10.0m).Value
        };

        foreach (var type in types)
        {
            if (existingCodeSet.Contains(type.Code))
            {
                continue;
            }

            context.VehicleTypes.Add(type);
            addedAny = true;
        }

        if (addedAny)
        {
            await context.SaveChangesAsync();
        }

        // 3. Seed AppConfig defaults
        if (!await context.AppConfigs.AnyAsync(c => c.Key == AppConfigKeys.TripDiscountPercent))
        {
            var configResult = AppConfig.Create(
                AppConfigKeys.TripDiscountPercent,
                "5",
                "Global trip discount percentage (0 = no discount)");

            if (configResult.IsFailure)
            {
                throw new InvalidOperationException($"Failed to create app config: {configResult.Error.Description}");
            }

            context.AppConfigs.Add(configResult.Value);
            await context.SaveChangesAsync();
        }

        // 4. Seed super admin
        await SeedSuperAdminAsync();

        // 4. Seed admin driver (fixed GUID so idempotent)
        await SeedAdminDriverAsync();

        if (environment.IsDevelopment())
        {
            await SeedDevelopmentOnlyAsync();
        }
    }

    private async Task SeedSuperAdminAsync()
    {
        var adminUser = await userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == SuperAdminPhone);
        if (adminUser != null)
        {
            return;
        }

        // 1. Create Identity user for Super Admin
        adminUser = new AppUser
        {
            Id = SuperAdminUserId.ToString(),
            UserName = SuperAdminPhone,
            PhoneNumber = SuperAdminPhone,
            Email = "admin@fat7i.dev",
            EmailConfirmed = true,
            PhoneNumberConfirmed = true
        };
        var identityResult = await userManager.CreateAsync(adminUser, SuperAdminPassword);
        if (!identityResult.Succeeded)
        {
            var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create super admin identity user: {errors}");
        }

        // 2. Assign roles: Admin, Driver, Passenger
        await userManager.AddToRoleAsync(adminUser, "Admin");
        await userManager.AddToRoleAsync(adminUser, "Driver");
        await userManager.AddToRoleAsync(adminUser, "Passenger");

        // 3. Create domain User
        var domainUserResult = User.Create(
            SuperAdminUserId,
            "Super Admin", "مدير النظام", "Super Administrateur",
            SuperAdminPhone,
            adminUser.Email,
            UserRole.Admin);

        if (domainUserResult.IsFailure)
        {
            throw new InvalidOperationException($"Failed to create super admin domain user: {domainUserResult.Error.Description}");
        }

        context.DomainUsers.Add(domainUserResult.Value);
        await context.SaveChangesAsync();
    }

    private async Task SeedAdminDriverAsync()
    {
        var driverExists = await context.Drivers.AnyAsync(d => d.Id == AdminDriverId);
        if (driverExists)
        {
            return;
        }

        // Create Identity user for the admin driver
        var phone = "+10000000000";
        var identityUser = new AppUser
        {
            Id = AdminDriverUserId.ToString(),
            UserName = phone,
            PhoneNumber = phone
        };
        var identityResult = await userManager.CreateAsync(identityUser, "Driver@123!");
        if (!identityResult.Succeeded)
        {
            var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
            throw new InvalidOperationException($"Failed to create admin driver identity user: {errors}");
        }

        await userManager.AddToRoleAsync(identityUser, "Driver");

        // Create domain User
        var domainUserResult = User.Create(
            AdminDriverUserId,
            "Admin Driver", "السائق الإداري", "Admin Chauffeur",
            phone,
            null,
            UserRole.Driver);

        if (domainUserResult.IsFailure)
        {
            throw new InvalidOperationException($"Failed to create admin driver domain user: {domainUserResult.Error.Description}");
        }

        context.DomainUsers.Add(domainUserResult.Value);
        await context.SaveChangesAsync();

        // Create Driver profile
        var driverResult = Driver.Create(AdminDriverId, AdminDriverUserId, "ADMIN-LIC-0001");

        if (driverResult.IsFailure)
        {
            throw new InvalidOperationException($"Failed to create admin driver profile: {driverResult.Error.Description}");
        }

        driverResult.Value.Activate();
        context.Drivers.Add(driverResult.Value);
        await context.SaveChangesAsync();
    }

    private Task SeedDevelopmentOnlyAsync()
    {
        return Task.CompletedTask;
    }
}

