using System.Net.Http;

using Microsoft.AspNetCore.Mvc.Testing;

using Taxi.Testing;

using Xunit;

namespace Taxi.Api.IntegrationTests.Infrastructure;

public sealed class ApiTestFixture : IAsyncLifetime
{
    private readonly PostgresTestContainer postgres = new();

    public TestWebApplicationFactory Factory { get; private set; } = default!;

    public HttpClient CreateClient()
        => Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = true });

    public async Task InitializeAsync()
    {
        await postgres.StartAsync();
        Factory = new TestWebApplicationFactory(postgres.ConnectionString);
        _ = CreateClient();
    }

    public async Task DisposeAsync()
    {
        Factory.Dispose();
        await postgres.DisposeAsync();
    }
}
