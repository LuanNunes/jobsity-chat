using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace com.jobsite.chat.Tests.Api.Integration;

// Integration specs for the CORS policy the SPA depends on (spec §3.1, §7.2 #8/#9). RED today:
// the Razor host has no "Spa" CORS policy, so the preflight response lacks the CORS headers.
public sealed class CorsPreflightTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private HttpClient NewClient() =>
        factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

    // §7.2 #8: preflight OPTIONS /api/rooms with the SPA origin + POST method ->
    // 2xx/204 echoing Access-Control-Allow-Origin: http://localhost:3000 and Allow-Credentials: true.
    [Fact]
    public async Task Preflight_FromSpaOrigin_AllowsOriginAndCredentials()
    {
        HttpClient client = NewClient();
        HttpRequestMessage request = new(HttpMethod.Options, "/api/rooms");
        request.Headers.Add("Origin", factory.SpaOrigin);
        request.Headers.Add("Access-Control-Request-Method", "POST");

        HttpResponseMessage response = await client.SendAsync(request);

        Assert.True(
            response.StatusCode == HttpStatusCode.NoContent || response.StatusCode == HttpStatusCode.OK,
            $"Expected 204 or 200 for preflight, got {(int)response.StatusCode}.");

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Origin", out System.Collections.Generic.IEnumerable<string>? allowedOrigins));
        Assert.Contains(factory.SpaOrigin, allowedOrigins!);

        Assert.True(response.Headers.TryGetValues("Access-Control-Allow-Credentials", out System.Collections.Generic.IEnumerable<string>? allowCredentials));
        Assert.Contains("true", allowCredentials!.Select(value => value.ToLowerInvariant()));
    }

    // §7.2 #9 (negative): a foreign origin is NOT echoed in Access-Control-Allow-Origin.
    [Fact]
    public async Task Preflight_FromForeignOrigin_DoesNotEchoAllowOrigin()
    {
        HttpClient client = NewClient();
        HttpRequestMessage request = new(HttpMethod.Options, "/api/rooms");
        request.Headers.Add("Origin", "http://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        HttpResponseMessage response = await client.SendAsync(request);

        bool echoedEvil =
            response.Headers.TryGetValues("Access-Control-Allow-Origin", out System.Collections.Generic.IEnumerable<string>? origins)
            && origins!.Contains("http://evil.example");
        Assert.False(echoedEvil);
    }
}
