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
    public async Task SendAndVerifyOtp_ReturnsTokens()
    {
        var phone = "+10000000001";

        var sendResponse = await client.PostAsJsonAsync("/api/v1/auth/send-otp", new { phone });
        sendResponse.EnsureSuccessStatusCode();

        var sendPayload = await sendResponse.Content.ReadFromJsonAsync<SendOtpResponse>(JsonOptions);

        Assert.NotNull(sendPayload);
        Assert.False(string.IsNullOrWhiteSpace(sendPayload!.SessionToken));

        var verifyResponse = await client.PostAsJsonAsync("/api/v1/auth/verify-otp", new { phone, sessionToken = sendPayload.SessionToken, code = "1234" });
        verifyResponse.EnsureSuccessStatusCode();

        var authPayload = await verifyResponse.Content.ReadFromJsonAsync<AuthResponseDto>(JsonOptions);

        Assert.NotNull(authPayload);
        Assert.False(string.IsNullOrWhiteSpace(authPayload!.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(authPayload.RefreshToken));
        Assert.Equal(phone, authPayload.User.Phone);
    }

    private sealed record SendOtpResponse(string SessionToken);

    private sealed record AuthResponseDto(string AccessToken, string RefreshToken, UserDto User);

    private sealed record UserDto(Guid Id, string Name, string Phone, string Role, string? Email, string? ProfilePhotoUrl);
}
