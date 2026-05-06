using System.Net;
using System.Text.Json;

using Taxi.Api.IntegrationTests.Infrastructure;

using Xunit;

namespace Taxi.Api.IntegrationTests.Vehicles;

[Collection("ApiTestCollection")]
public class GetVehiclesTests(ApiTestFixture fixture)
{
    private readonly HttpClient client = fixture.CreateClient();

    [Fact]
    public async Task GetVehicles_ReturnsOkAndArray()
    {
        var response = await client.GetAsync("/api/v1/vehicles");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
    }
}
