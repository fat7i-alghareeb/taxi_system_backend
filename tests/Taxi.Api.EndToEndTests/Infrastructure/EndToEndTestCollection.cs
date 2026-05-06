using Xunit;

namespace Taxi.Api.EndToEndTests.Infrastructure;

[CollectionDefinition("EndToEndTestCollection")]
public sealed class EndToEndTestCollection : ICollectionFixture<EndToEndTestFixture>
{
}
