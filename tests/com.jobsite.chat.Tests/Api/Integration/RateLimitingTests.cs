using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace com.jobsite.chat.Tests.Api.Integration;

// Integration specs for auth-endpoint rate limiting (plan §2). RED today: the host installs no
// rate limiter, so repeated login attempts return 401 forever, never 429. Goes GREEN once
// Program.cs calls AddRateLimiter with a named "auth" policy (fixed window per client IP) applied
// to the /api/auth group via RequireRateLimiting("auth"), reading its permit-per-window from the
// RateLimiting config section and rejecting with 429.
//
// Determinism: a derived factory overrides RateLimiting:AuthPermitPerMinute to a small value so the
// limit trips predictably. In the in-process TestServer requests share a connection with no real
// RemoteIpAddress (null -> a single stable partition key), so the whole burst lands in one
// partition and the limit still trips.
public sealed class RateLimitingTests
{
    // The small per-IP auth limit this suite pins. Implementer contract: the "auth" policy MUST
    // read RateLimiting:AuthPermitPerMinute (fall back to the plan default ~10 if absent).
    private const int AuthLimit = 3;

    // Standalone factory (the shared ApiWebApplicationFactory is sealed): mirrors that harness's
    // isolation — unique throwaway SQLite db per instance, Development env, pinned SPA CORS origin —
    // and additionally pins the small auth rate limit so the limiter trips deterministically.
    private sealed class SmallAuthLimitFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath =
            Path.Combine(Path.GetTempPath(), $"jobsity-chat-ratelimit-test-{Guid.NewGuid():N}.db");

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Development);

            builder.ConfigureAppConfiguration((context, configuration) =>
            {
                Dictionary<string, string?> overrides = new()
                {
                    ["ConnectionStrings:ChatDatabase"] = $"Data Source={_dbPath}",
                    ["Cors:AllowedOrigin"] = "http://localhost:3000",
                    ["RateLimiting:AuthPermitPerMinute"] = AuthLimit.ToString(),
                };
                configuration.AddInMemoryCollection(overrides);
            });
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (!disposing)
            {
                return;
            }

            foreach (string suffix in new[] { string.Empty, "-wal", "-shm", "-journal" })
            {
                string path = _dbPath + suffix;
                if (File.Exists(path))
                {
                    try
                    {
                        File.Delete(path);
                    }
                    catch (IOException)
                    {
                        // Best-effort cleanup of the throwaway db.
                    }
                }
            }
        }
    }

    private static HttpClient NewClient(SmallAuthLimitFactory factory) =>
        factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

    private static object LoginBody() =>
        new { email = "nobody@example.com", password = "WrongPass1!", rememberMe = false };

    // Sending more than AuthLimit login attempts within the window -> the request past the limit is
    // 429 TooManyRequests. Earlier over-the-limit attempts are 401 (invalid creds) — the assertion
    // is the transition to 429.
    [Fact]
    public async Task PostLogin_ExceedingAuthLimit_Returns429TooManyRequests()
    {
        using SmallAuthLimitFactory factory = new();
        HttpClient client = NewClient(factory);

        // Exhaust the window: these are 401 (invalid credentials), NOT rate-limited yet.
        for (int attempt = 0; attempt < AuthLimit; attempt++)
        {
            HttpResponseMessage allowed = await client.PostAsJsonAsync("/api/auth/login", LoginBody());
            Assert.NotEqual(HttpStatusCode.TooManyRequests, allowed.StatusCode);
        }

        // The next request in the same window crosses the limit -> 429.
        HttpResponseMessage rejected = await client.PostAsJsonAsync("/api/auth/login", LoginBody());

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

    // Register shares the /api/auth group, so it is throttled by the same "auth" policy. A burst
    // over the limit eventually yields 429 rather than continuing to process registrations.
    [Fact]
    public async Task PostRegister_ExceedingAuthLimit_EventuallyReturns429()
    {
        using SmallAuthLimitFactory factory = new();
        HttpClient client = NewClient(factory);

        bool saw429 = false;
        for (int attempt = 0; attempt <= AuthLimit; attempt++)
        {
            HttpResponseMessage response = await client.PostAsJsonAsync(
                "/api/auth/register",
                new { email = $"burst-{attempt}@example.com", displayName = "Burst", password = "Passw0rd!" });

            if (response.StatusCode == HttpStatusCode.TooManyRequests)
            {
                saw429 = true;
                break;
            }
        }

        Assert.True(saw429, $"Expected a 429 within {AuthLimit + 1} rapid /api/auth/register calls.");
    }

    // Health is exempt from the limiter: the same burst that trips /api/auth never 429s /health.
    [Fact]
    public async Task GetHealth_UnderAuthBurst_IsNotRateLimited()
    {
        using SmallAuthLimitFactory factory = new();
        HttpClient client = NewClient(factory);

        for (int attempt = 0; attempt <= AuthLimit + 2; attempt++)
        {
            HttpResponseMessage response = await client.GetAsync("/health");
            Assert.NotEqual(HttpStatusCode.TooManyRequests, response.StatusCode);
        }
    }
}
