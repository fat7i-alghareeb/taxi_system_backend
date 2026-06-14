using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;

using Taxi.Domain.Admins;
using Taxi.Domain.Configuration;
using Taxi.Domain.Vehicles;
using Taxi.Infrastructure.Identity;

namespace Taxi.Infrastructure.Data;

public class ApplicationDbContextInitialiser(
    AppDbContext context,
    RoleManager<IdentityRole> roleManager,
    UserManager<AppUser> userManager,
    IHostEnvironment environment)
{
    private const string AdminUserName = "admin";
    private const string AdminPassword = "admin";
    private const string AdminDisplayName = "Admin";
    private const string AdminEmail = "admin@fat7i.dev";
    private const string SecondAdminUserName = "admin2";
    private const string SecondAdminPassword = "admin2";
    private const string SecondAdminDisplayName = "Second Admin";
    private const string SecondAdminEmail = "admin2@fat7i.dev";

    public async Task SeedAsync()
    {
        await TrySeedAsync();
    }

    /// <summary>
    /// Drops the entire database and re-applies all migrations.
    /// DESTRUCTIVE — only meant for local development resets.
    /// Caller is responsible for gating this behind an environment / config check.
    /// </summary>
    public async Task ResetDatabaseAsync()
    {
        await context.Database.EnsureDeletedAsync();
        await context.Database.MigrateAsync();
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
            VehicleType.Create(Guid.NewGuid(), "standard", "Standard", "عادي", "Standaard", "Standard", "Standard", "Standard", "Standard", "Standard", "Standard", 4, 2.8m, 0.20m, 5.0m, 1).Value,
            VehicleType.Create(Guid.NewGuid(), "xl", "XL Van", "فان كبير", "XL Van", "XL Van", "XL Van", "XL Van", "XL Van", "XL Van", "XL Van", 8, 3.20m, 0.25m, 8.0m, 2).Value,
            VehicleType.Create(Guid.NewGuid(), "wheelchair", "Wheelchair Taxi", "تاكسي ذوي الاحتياجات الخاصة", "Rolstoel Taxi", "Wheelchair Taxi", "Wheelchair Taxi", "Wheelchair Taxi", "Wheelchair Taxi", "Wheelchair Taxi", "Wheelchair Taxi", 4, 3.50m, 0.25m, 10.0m, 3).Value
        };

        foreach (var type in types)
        {
            var existing = await context.VehicleTypes.FirstOrDefaultAsync(v => v.Code == type.Code);
            if (existing != null)
            {
                if (existing.SortOrder != type.SortOrder)
                {
                    existing.UpdateSortOrder(type.SortOrder);
                    addedAny = true;
                }

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

        if (!await context.AppConfigs.AnyAsync(c => c.Key == AppConfigKeys.Currency))
        {
            var currencyResult = AppConfig.Create(
                AppConfigKeys.Currency,
                "EUR",
                "Global ISO 4217 currency code used for all Stripe payments (assumes a 2-decimal currency).");

            if (currencyResult.IsFailure)
            {
                throw new InvalidOperationException($"Failed to create app config: {currencyResult.Error.Description}");
            }

            context.AppConfigs.Add(currencyResult.Value);
            await context.SaveChangesAsync();
        }

        // Company contact details printed in the invoice footer (admin-editable).
        await SeedCompanyContactAsync(AppConfigKeys.CompanyEmail, "info@fat7i.dev", "Company email shown on invoices.");
        await SeedCompanyContactAsync(AppConfigKeys.CompanyPhone, "0639550352", "Company phone shown on invoices.");
        await SeedCompanyContactAsync(AppConfigKeys.CompanyWebsite, "www.fat7i.dev", "Company website shown on invoices.");

        // 4. Seed admin users
        await SeedAdminAsync(AdminUserName, AdminPassword, AdminDisplayName, AdminEmail);
        await SeedAdminAsync(SecondAdminUserName, SecondAdminPassword, SecondAdminDisplayName, SecondAdminEmail);

        if (environment.IsDevelopment())
        {
            await SeedDevelopmentOnlyAsync();
        }
    }

    private async Task SeedCompanyContactAsync(string key, string value, string description)
    {
        if (await context.AppConfigs.AnyAsync(c => c.Key == key))
        {
            return;
        }

        var result = AppConfig.Create(key, value, description);
        if (result.IsFailure)
        {
            throw new InvalidOperationException($"Failed to seed app config '{key}': {result.Error.Description}");
        }

        context.AppConfigs.Add(result.Value);
        await context.SaveChangesAsync();
    }

    private async Task SeedAdminAsync(string userName, string password, string displayName, string email)
    {
        var adminUser = await userManager.FindByNameAsync(userName);

        if (adminUser == null)
        {
            adminUser = new AppUser
            {
                UserName = userName,
                Email = email,
                EmailConfirmed = true,
                RequiresPasswordReset = true,
            };

            var identityResult = await userManager.CreateAsync(adminUser, password);
            if (!identityResult.Succeeded)
            {
                var errors = string.Join(", ", identityResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to create admin identity user '{userName}': {errors}");
            }
        }

        if (adminUser.Email != email || !adminUser.EmailConfirmed)
        {
            adminUser.Email = email;
            adminUser.EmailConfirmed = true;
            var updateResult = await userManager.UpdateAsync(adminUser);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to update admin identity user '{userName}': {errors}");
            }
        }

        var usesSeedPassword = await userManager.CheckPasswordAsync(adminUser, password);
        if (usesSeedPassword && !adminUser.RequiresPasswordReset)
        {
            adminUser.RequiresPasswordReset = true;
            var updateResult = await userManager.UpdateAsync(adminUser);
            if (!updateResult.Succeeded)
            {
                var errors = string.Join(", ", updateResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to update admin identity user '{userName}': {errors}");
            }
        }

        if (!await userManager.IsInRoleAsync(adminUser, "Admin"))
        {
            await userManager.AddToRoleAsync(adminUser, "Admin");
        }

        var roles = await userManager.GetRolesAsync(adminUser);
        var rolesToRemove = roles.Where(role => role != "Admin").ToArray();
        if (rolesToRemove.Length > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(adminUser, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                var errors = string.Join(", ", removeResult.Errors.Select(e => e.Description));
                throw new InvalidOperationException($"Failed to remove non-admin roles from '{userName}': {errors}");
            }
        }

        var adminUserId = Guid.Parse(adminUser.Id);
        var adminProfile = await context.AdminProfiles.FirstOrDefaultAsync(a => a.Id == adminUserId);
        if (adminProfile is null)
        {
            var adminProfileResult = AdminProfile.Create(adminUserId, displayName, email);
            if (adminProfileResult.IsFailure)
            {
                throw new InvalidOperationException($"Failed to create admin profile '{userName}': {adminProfileResult.Error.Description}");
            }

            context.AdminProfiles.Add(adminProfileResult.Value);
            await context.SaveChangesAsync();
        }
        else if (adminProfile.Email != email || adminProfile.Name != displayName)
        {
            var updateResult = adminProfile.Update(displayName, email, adminProfile.Phone1, adminProfile.Phone2);
            if (updateResult.IsError)
            {
                throw new InvalidOperationException($"Failed to update admin profile '{userName}': {updateResult.Error.Description}");
            }

            await context.SaveChangesAsync();
        }
    }

    private Task SeedDevelopmentOnlyAsync()
    {
        return Task.CompletedTask;
    }
}

