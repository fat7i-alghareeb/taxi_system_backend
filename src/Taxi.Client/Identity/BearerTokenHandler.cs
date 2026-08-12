using System.Net.Http.Headers;
using Blazored.LocalStorage;

namespace Taxi.Client.Identity;

/// <summary>
/// Attaches the admin bearer token to outgoing API calls.
///
/// SECURITY — KNOWN LIMITATION: the token lives in <c>localStorage</c>, which is readable by
/// any script running in this origin. An admin token grants wallet adjustments, refunds and
/// trip takeover, so an XSS anywhere in the admin origin is a full admin compromise.
///
/// This is currently unexploited because this Blazor client is not deployed (it appears in
/// no docker-compose service and no Caddy route). Do not ship it without fixing this first.
///
/// Real fix, in order of preference:
///  1. Move admin sessions to an <c>HttpOnly; Secure; SameSite=Strict</c> cookie issued by the
///     API. This requires an API-side auth change, so it is deliberately not done here.
///  2. Failing that, serve the client with a strict CSP and use session storage so the token
///     dies with the tab. That narrows the window; it does not close it — session storage is
///     just as script-readable as local storage.
/// </summary>
public class BearerTokenHandler(ILocalStorageService localStorage) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await localStorage.GetItemAsync<string>("authToken", cancellationToken);

        if (!string.IsNullOrEmpty(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        return await base.SendAsync(request, cancellationToken);
    }
}

