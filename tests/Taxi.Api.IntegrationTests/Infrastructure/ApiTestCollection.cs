using Xunit;

namespace Taxi.Api.IntegrationTests.Infrastructure;

[CollectionDefinition("ApiTestCollection")]
public sealed class ApiTestCollection : ICollectionFixture<ApiTestFixture>
{
}
