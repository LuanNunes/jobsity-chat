using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace com.jobsite.chat.Tests.Api.Integration;

public sealed class CorsPreflightTests(ApiWebApplicationFactory factory) : IClassFixture<ApiWebApplicationFactory>
{
    private HttpClient NewClient() =>
        factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

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
