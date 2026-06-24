using System.Net.Http.Json;
using System.Text.Json;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;

using Taxi.Api.IntegrationTests.Infrastructure;

using Xunit;

namespace Taxi.Api.IntegrationTests.Hubs;

[Collection("ApiTestCollection")]
public sealed class TripHubAuthorizationTests(ApiTestFixture fixture)
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    [Fact]
    public async Task Admin_CanJoinTripGroup()
    {
        var token = await LoginAsync("+963900000000");
        await using var connection = CreateConnection(token);
        await connection.StartAsync();

        await connection.InvokeAsync("JoinTripGroup", Guid.NewGuid());
    }

    [Fact]
    public async Task UnrelatedPassenger_CannotJoinTripGroup()
    {
        var token = await LoginAsync("+963911111111");
        await using var connection = CreateConnection(token);
        await connection.StartAsync();

        var exception = await Assert.ThrowsAsync<HubException>(
            () => connection.InvokeAsync("JoinTripGroup", Guid.NewGuid()));

        Assert.Contains("not authorized", exception.Message, StringComparison.OrdinalIgnoreCase);
    }

    private HubConnection CreateConnection(string token)
    {
        var client = fixture.CreateClient();
        return new HubConnectionBuilder()
            .WithUrl(
                new Uri(client.BaseAddress!, "/hubs/trips"),
                options =>
                {
                    options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                    options.Transports = HttpTransportType.LongPolling;
                    options.HttpMessageHandlerFactory = _ => fixture.Factory.Server.CreateHandler();
                })
            .Build();
    }

    private async Task<string> LoginAsync(string phone)
    {
        var client = fixture.CreateClient();
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
