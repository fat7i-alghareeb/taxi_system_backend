using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Taxi.Api.IntegrationTests.Infrastructure;

using Xunit;

namespace Taxi.Api.IntegrationTests.Drivers;

[Collection("ApiTestCollection")]
public class GetDriversTests(ApiTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    [Fact]
    public async Task GetDrivers_ReturnsOkAndArray()
    {
        var client = fixture.CreateClient();
        var token = await LoginAsAdminAsync(client);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/v1/drivers");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadAsStringAsync();
        using var document = JsonDocument.Parse(payload);
        Assert.Equal(JsonValueKind.Array, document.RootElement.ValueKind);
    }

    private static async Task<string> LoginAsAdminAsync(HttpClient client)
    {
        const string phone = "+963900000000";

        var response = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { phone, firebaseIdToken = phone, fcmToken = (string?)null });

        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);

        Assert.NotNull(payload);
        return payload!.AccessToken;
    }

    private sealed record AuthResponseDto(string AccessToken);
}
