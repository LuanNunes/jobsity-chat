using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace com.jobsite.chat.Tests.Api.Integration;

// Integration specs for the operational health probes (plan §3). RED today: the host maps no
// /health endpoints, so every request 404s. Goes GREEN once Program.cs calls
// MapHealthChecks("/health") (liveness) and MapHealthChecks("/health/ready") (readiness), with
// both exempt from auth and the rate limiter.
public sealed class HealthEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public HealthEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient NewClient() =>
        _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

    // Liveness: GET /health -> 200 OK (no dependency probing).
    [Fact]
    public async Task GetHealth_Liveness_Returns200()
    {
        HttpClient client = NewClient();

        HttpResponseMessage response = await client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Liveness is anonymous: /health must NOT redirect to a login flow and must NOT 401.
    // The auth pivot guarantee is 401-not-302; here we additionally require the probe is public.
    [Fact]
    public async Task GetHealth_WithoutAuthentication_IsPublicAndNot401Or302()
    {
        HttpClient client = NewClient();

        HttpResponseMessage response = await client.GetAsync("/health");

        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Found, response.StatusCode);
        Assert.Null(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // Liveness must be exempt from the rate limiter: a burst well over any auth/global limit never
    // yields 429 on /health.
    [Fact]
    public async Task GetHealth_UnderBurst_IsNeverRateLimited()
    {
        HttpClient client = NewClient();

        for (int attempt = 0; attempt < 50; attempt++)
        {
            HttpResponseMessage response = await client.GetAsync("/health");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }

    // Readiness: GET /health/ready probes dependencies. In the in-process test host RabbitMQ is
    // NOT available, so readiness reports unhealthy -> 503 ServiceUnavailable. This documents that
    // readiness actually probes dependencies (unlike liveness). It must still be a mapped endpoint
    // (503, not 404) and must not require auth (not 401) or redirect (not 302).
    [Fact]
    public async Task GetHealthReady_WithoutRabbitMq_Returns503ServiceUnavailable()
    {
        HttpClient client = NewClient();

        HttpResponseMessage response = await client.GetAsync("/health/ready");

        Assert.Equal(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.NotFound, response.StatusCode);
        Assert.NotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }
}
