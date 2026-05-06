using Testcontainers.PostgreSql;

namespace Taxi.Testing;

public sealed class PostgresTestContainer : IAsyncDisposable
{
    private readonly PostgreSqlContainer container = new PostgreSqlBuilder()
        .WithDatabase("taxi_tests")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public string ConnectionString => container.GetConnectionString();

    public Task StartAsync() => container.StartAsync();

    public async ValueTask DisposeAsync()
    {
        await container.DisposeAsync();
    }
}
