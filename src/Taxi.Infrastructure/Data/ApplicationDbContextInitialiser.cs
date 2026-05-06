using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

using Taxi.Domain.Drivers;
using Taxi.Domain.Users;
using Taxi.Domain.Vehicles;
using Taxi.Infrastructure.Identity;

namespace Taxi.Infrastructure.Data;

public class ApplicationDbContextInitialiser(
    AppDbContext context,
    RoleManager<IdentityRole> roleManager)
{
    public async Task InitialiseAsync()
    {
        // For development/reset
        // await context.Database.EnsureDeletedAsync();
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
            // Log error in a real app
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
                await roleManager.CreateAsync(new IdentityRole(roleName));
            }
        }

        // 2. Seed Vehicle Types
        if (!await context.VehicleTypes.AnyAsync())
        {
            var types = new List<VehicleType>
            {
                VehicleType.Create(Guid.NewGuid(), "standard", "Standard", "عادي", "Standaard", 4, 2.8m, 0.20m, 5.0m).Value,
                VehicleType.Create(Guid.NewGuid(), "xl", "XL Van", "فان كبير", "XL Van", 8, 3.20m, 0.25m, 8.0m).Value,
                VehicleType.Create(Guid.NewGuid(), "wheelchair", "Wheelchair Taxi", "تاكسي ذوي الاحتياجات الخاصة", "Rolstoel Taxi", 4, 3.50m, 0.25m, 10.0m).Value
            };

            context.VehicleTypes.AddRange(types);
            await context.SaveChangesAsync();
        }
    }
}
