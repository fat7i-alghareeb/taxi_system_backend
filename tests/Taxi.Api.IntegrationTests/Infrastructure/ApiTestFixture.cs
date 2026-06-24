using System.Net.Http;

using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Admins;
using Taxi.Infrastructure.Data;
using Taxi.Infrastructure.Identity;
using Taxi.Testing;

using Xunit;

namespace Taxi.Api.IntegrationTests.Infrastructure;

public sealed class ApiTestFixture : IAsyncLifetime
{
    private readonly PostgresTestContainer postgres = new();
    private HttpClient? startupClient;

    public TestWebApplicationFactory Factory { get; private set; } = default!;

    public HttpClient CreateClient()
        => Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();
        Factory = new TestWebApplicationFactory(postgres.ConnectionString);
        await SeedAdminAsync();
        startupClient = CreateClient();
    }

    public async Task DisposeAsync()
    {
        startupClient?.Dispose();
        Factory.Dispose();
        await postgres.DisposeAsync();
    }

    private async Task SeedAdminAsync()
    {
        const string phone = "+963900000000";
        await using var scope = Factory.Services.CreateAsyncScope();
        var identityService = scope.ServiceProvider.GetRequiredService<IIdentityService>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<AppUser>>();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var identityResult = await identityService.GetOrCreateUserByPhoneAsync(phone, "Admin");
        if (identityResult.IsFailure)
        {
            throw new InvalidOperationException("Could not seed integration admin identity.");
        }

        var adminId = Guid.Parse(identityResult.Value);
        var identity = await userManager.FindByIdAsync(identityResult.Value)
            ?? throw new InvalidOperationException("Seeded integration identity was not found.");
        if (!await userManager.IsInRoleAsync(identity, "Admin"))
        {
            var roleResult = await userManager.AddToRoleAsync(identity, "Admin");
            if (!roleResult.Succeeded)
            {
                throw new InvalidOperationException("Could not assign integration admin role.");
            }
        }

        if (!await context.AdminProfiles.AnyAsync(admin => admin.Id == adminId))
        {
            var admin = AdminProfile.Create(
                adminId,
                "Integration Admin",
                "integration-admin@fat7i.dev",
                phone);
            if (admin.IsFailure)
            {
                throw new InvalidOperationException("Could not create integration admin profile.");
            }

            context.AdminProfiles.Add(admin.Value);
            await context.SaveChangesAsync();
        }
    }
}
