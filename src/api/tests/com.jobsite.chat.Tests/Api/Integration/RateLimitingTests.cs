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

public sealed class RateLimitingTests
{

    private const int AuthLimit = 3;

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

    [Fact]
    public async Task PostLogin_ExceedingAuthLimit_Returns429TooManyRequests()
    {
        using SmallAuthLimitFactory factory = new();
        HttpClient client = NewClient(factory);

        for (int attempt = 0; attempt < AuthLimit; attempt++)
        {
            HttpResponseMessage allowed = await client.PostAsJsonAsync("/api/auth/login", LoginBody());
            Assert.NotEqual(HttpStatusCode.TooManyRequests, allowed.StatusCode);
        }

        HttpResponseMessage rejected = await client.PostAsJsonAsync("/api/auth/login", LoginBody());

        Assert.Equal(HttpStatusCode.TooManyRequests, rejected.StatusCode);
    }

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
