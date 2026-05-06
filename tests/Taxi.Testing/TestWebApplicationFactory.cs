using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

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
                ["AppSettings:GoogleMapsApiKey"] = "",
            };

            config.AddInMemoryCollection(settings);
        });
    }
}
