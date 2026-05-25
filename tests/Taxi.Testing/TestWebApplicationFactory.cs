using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Taxi.Application.Common.Interfaces;
using Taxi.Domain.Common.Results;

namespace Taxi.Testing;

public sealed class TestWebApplicationFactory(string connectionString) : WebApplicationFactory<Program>
{
    private readonly string connectionString = connectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = connectionString,
                ["JwtSettings:Secret"] = "TestJwtSecret_TestJwtSecret_TestJwtSecret_123",
                ["JwtSettings:Issuer"] = "TaxiServer",
                ["JwtSettings:Audience"] = "TaxiClient",
                ["AppSettings:CorsPolicyName"] = "TaxiCorsPolicy",
                ["AppSettings:AllowedOrigins:0"] = "http://localhost",
                ["AppSettings:DefaultLanguage"] = "en",
                ["AppSettings:GoogleMapsApiKey"] = string.Empty,
            };

            config.AddInMemoryCollection(settings);
        });

        builder.ConfigureTestServices(services =>
        {
            services.RemoveAll<IFirebaseAuthService>();
            services.AddSingleton<IFirebaseAuthService, TestFirebaseAuthService>();
        });
    }

    private sealed class TestFirebaseAuthService : IFirebaseAuthService
    {
        public Task<Result<string>> VerifyIdTokenAndGetPhoneAsync(string idToken, CancellationToken ct = default)
            => Task.FromResult<Result<string>>(idToken);
    }
}
