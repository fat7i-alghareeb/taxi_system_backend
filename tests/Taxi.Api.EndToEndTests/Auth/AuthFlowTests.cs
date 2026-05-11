using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using Taxi.Api.EndToEndTests.Infrastructure;

using Xunit;

namespace Taxi.Api.EndToEndTests.Auth;

[Collection("EndToEndTestCollection")]
public class AuthFlowTests(EndToEndTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };
    private readonly HttpClient client = fixture.CreateClient();

    [Fact]
    public async Task LoginAndRefresh_RotatesRefreshToken()
    {
        var phone = "+10000000001";

        var verifyResponse = await client.PostAsJsonAsync(
            "/api/v1/auth/login",
            new { phone, firebaseIdToken = phone, fcmToken = (string?)null });

        verifyResponse.EnsureSuccessStatusCode();
        var authPayload = await verifyResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);

        Assert.NotNull(authPayload);
        Assert.False(string.IsNullOrWhiteSpace(authPayload!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(authPayload.RefreshToken));
        Assert.Equal(phone, authPayload.User.Phone);

        var refreshResponse = await client.PostAsJsonAsync(
            "/api/v1/identity/tokens/refresh",
            new { expiredAccessToken = authPayload.AccessToken, refreshToken = authPayload.RefreshToken });

        refreshResponse.EnsureSuccessStatusCode();
        var refreshedPayload = await refreshResponse.Content.ReadFromJsonAsync<TokenResponseDto>(JsonOptions);

        Assert.NotNull(refreshedPayload);
        Assert.False(string.IsNullOrWhiteSpace(refreshedPayload!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(refreshedPayload.RefreshToken));
        Assert.NotEqual(authPayload.RefreshToken, refreshedPayload.RefreshToken);
        Assert.True(refreshedPayload.ExpiresOnUtc > DateTimeOffset.UtcNow);

        var reusedOldRefreshResponse = await client.PostAsJsonAsync(
            "/api/v1/identity/tokens/refresh",
            new { expiredAccessToken = authPayload.AccessToken, refreshToken = authPayload.RefreshToken });

        Assert.Equal(HttpStatusCode.Conflict, reusedOldRefreshResponse.StatusCode);
    }

    private sealed record AuthResponseDto(string AccessToken, string RefreshToken, UserDto User);

    private sealed record TokenResponseDto(string AccessToken, string RefreshToken, DateTimeOffset ExpiresOnUtc);

    private sealed record UserDto(Guid Id, string? Name, string Phone, string Role, string? Email, string? ProfilePhotoUrl);
}
