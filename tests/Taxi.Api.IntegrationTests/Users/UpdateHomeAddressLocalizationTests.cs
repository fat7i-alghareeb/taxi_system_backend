using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using Taxi.Api.IntegrationTests.Infrastructure;

using Xunit;

namespace Taxi.Api.IntegrationTests.Users;

[Collection("ApiTestCollection")]
public class UpdateHomeAddressLocalizationTests(ApiTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    // Regression test: under a Dutch request culture, ASP.NET Core's default
    // decimal model binder used to treat the '.' in an invariant-formatted
    // coordinate as a thousands separator, silently corrupting it into a
    // huge number that failed the lat/lng range check for every address.
    [Fact]
    public async Task UpdateProfile_WithDutchAcceptLanguage_SavesInvariantCoordinates()
    {
        var client = fixture.CreateClient();
        var token = await LoginAsync(client, "+31600000001");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        client.DefaultRequestHeaders.AcceptLanguage.Clear();
        client.DefaultRequestHeaders.AcceptLanguage.Add(new StringWithQualityHeaderValue("nl"));

        using var form = new MultipartFormDataContent
        {
            { new StringContent("Set"), "HomeAddressOperation" },
            { new StringContent("Schiedamseweg, Netherlands"), "HomeAddressLabel" },
            { new StringContent("51.9225"), "HomeAddressLatitude" },
            { new StringContent("4.4792"), "HomeAddressLongitude" },
        };

        var response = await client.PostAsync("/api/v1/users/me", form);
        var payload = await response.Content.ReadAsStringAsync();

        Assert.True(response.IsSuccessStatusCode, payload);

        using var document = JsonDocument.Parse(payload);
        var root = document.RootElement;
        Assert.Equal(51.9225m, root.GetProperty("homeAddressLatitude").GetDecimal());
        Assert.Equal(4.4792m, root.GetProperty("homeAddressLongitude").GetDecimal());
        Assert.Equal("Schiedamseweg, Netherlands", root.GetProperty("homeAddressLabel").GetString());
    }

    private static async Task<string> LoginAsync(HttpClient client, string phone)
    {
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
