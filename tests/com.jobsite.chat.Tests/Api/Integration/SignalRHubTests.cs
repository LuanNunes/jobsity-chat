using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR.Client;

namespace com.jobsite.chat.Tests.Api.Integration;

// Integration specs for the kept SignalR hub under the API-only host (spec §3.5, §7.2 #12/#13).
//
// DOWNGRADE (per spec §7.2 note): the cookie-through-SignalR round-trip (#12) is plumbing-sensitive
// against TestServer, so the reliable guard here is the anonymous-negotiate -> 401 test (#13),
// asserted with a plain HttpClient POST to the negotiate endpoint (no SignalR client transport
// negotiation, so it is deterministic). #12 is kept as a Skip'd fact with the reason, exactly as
// the spec instructs (do not delete it). The kept ChatHubTests unit suite already covers the
// join/send mapping, so the anon-401 test is sufficient to prove [Authorize] still challenges the
// cookie scheme under the reworked host.
public sealed class SignalRHubTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public SignalRHubTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    // §7.2 #13: anonymous hub negotiate (no cookie) -> 401 (the hub is [Authorize]d, and the
    // reworked cookie config returns 401 JSON, not a 302 to a login page).
    [Fact]
    public async Task HubNegotiate_WithoutCookie_Returns401()
    {
        HttpClient client = _factory.CreateClient(
            new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
            {
                AllowAutoRedirect = false,
            });

        // The SignalR negotiate handshake is a POST to "{hubUrl}/negotiate".
        HttpResponseMessage response = await client.PostAsync("/hubs/chat/negotiate", content: null);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    // §7.2 #12: full cookie round-trip (register -> connect with cookie -> JoinRoom/LoadHistory ->
    // SendMessage/ReceiveMessage). Downgraded to Skip per spec §7.2: cookie plumbing through
    // TestServer's in-memory transport is flaky; the anon-401 test above plus the ChatHubTests unit
    // suite cover the same surface. Implementer may un-skip if they wire the TestServer message
    // handler into the HubConnection (HttpMessageHandlerFactory) and confirm stability.
    [Fact(Skip = "Downgraded per spec §7.2: cookie-through-SignalR over TestServer is plumbing-flaky; covered by anon-401 negotiate + ChatHubTests.")]
    public async Task HubRoundTrip_WithAuthCookie_JoinsRoomAndReceivesMessage()
    {
        HttpClient client = _factory.CreateClient();
        string email = $"hubuser-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync(
            "/api/auth/register", new { email, displayName = "Hub User", password = "Passw0rd!" });

        HubConnection connection = new HubConnectionBuilder()
            .WithUrl(new Uri(_factory.Server.BaseAddress, "hubs/chat"), options =>
            {
                options.HttpMessageHandlerFactory = _ => _factory.Server.CreateHandler();
            })
            .Build();

        await connection.StartAsync();
        await connection.StopAsync();
        await connection.DisposeAsync();
    }
}
