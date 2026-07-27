using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace com.jobsite.chat.Tests.Api.Integration;

public sealed class AuthEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public AuthEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient NewClient() =>
        _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

    private static object NewRegisterBody(string email, string displayName, string password) =>
        new { email, displayName, password };

    [Fact]
    public async Task Register_ValidRequest_Returns200WithAuthCookieAndUserJson()
    {
        HttpClient client = NewClient();
        string email = $"alice-{Guid.NewGuid():N}@example.com";

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register", NewRegisterBody(email, "Alice", "Passw0rd!"));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(response.Headers, header => header.Key == "Set-Cookie");

        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(body.GetProperty("id").GetString()));
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.Equal("Alice", body.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task Register_ThenAuthedRequest_Succeeds()
    {
        HttpClient client = NewClient();
        string email = $"bob-{Guid.NewGuid():N}@example.com";

        HttpResponseMessage register = await client.PostAsJsonAsync(
            "/api/auth/register", NewRegisterBody(email, "Bob", "Passw0rd!"));
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        HttpResponseMessage rooms = await client.GetAsync("/api/rooms");
        Assert.Equal(HttpStatusCode.OK, rooms.StatusCode);
    }

    [Fact]
    public async Task Me_WithRegisterCookie_ReturnsSameUserJson()
    {
        HttpClient client = NewClient();
        string email = $"carol-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync(
            "/api/auth/register", NewRegisterBody(email, "Carol", "Passw0rd!"));

        HttpResponseMessage response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(email, body.GetProperty("email").GetString());
        Assert.Equal("Carol", body.GetProperty("displayName").GetString());
    }

    [Fact]
    public async Task Register_DuplicateEmail_Returns400WithoutCookie()
    {
        HttpClient client = NewClient();
        string email = $"dupe-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync(
            "/api/auth/register", NewRegisterBody(email, "Dave", "Passw0rd!"));

        HttpResponseMessage second = await client.PostAsJsonAsync(
            "/api/auth/register", NewRegisterBody(email, "Dave2", "Passw0rd!"));

        Assert.Equal(HttpStatusCode.BadRequest, second.StatusCode);
        Assert.DoesNotContain(second.Headers, header => header.Key == "Set-Cookie");
        string content = await second.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public async Task Register_WeakPassword_Returns400WithErrors()
    {
        HttpClient client = NewClient();
        string email = $"weak-{Guid.NewGuid():N}@example.com";

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/register", NewRegisterBody(email, "Eve", "a"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.DoesNotContain(response.Headers, header => header.Key == "Set-Cookie");
        string content = await response.Content.ReadAsStringAsync();
        Assert.False(string.IsNullOrWhiteSpace(content));
    }

    [Fact]
    public async Task Login_ValidCredentials_Returns200WithCookie()
    {
        string email = $"frank-{Guid.NewGuid():N}@example.com";
        HttpClient registerClient = NewClient();
        await registerClient.PostAsJsonAsync(
            "/api/auth/register", NewRegisterBody(email, "Frank", "Passw0rd!"));

        HttpClient loginClient = NewClient();
        HttpResponseMessage response = await loginClient.PostAsJsonAsync(
            "/api/auth/login", new { email, password = "Passw0rd!", rememberMe = false });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains(response.Headers, header => header.Key == "Set-Cookie");
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        string email = $"grace-{Guid.NewGuid():N}@example.com";
        HttpClient registerClient = NewClient();
        await registerClient.PostAsJsonAsync(
            "/api/auth/register", NewRegisterBody(email, "Grace", "Passw0rd!"));

        HttpClient loginClient = NewClient();
        HttpResponseMessage response = await loginClient.PostAsJsonAsync(
            "/api/auth/login", new { email, password = "WrongPass1!", rememberMe = false });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_UnknownEmail_Returns401()
    {
        HttpClient client = NewClient();

        HttpResponseMessage response = await client.PostAsJsonAsync(
            "/api/auth/login",
            new { email = $"nobody-{Guid.NewGuid():N}@example.com", password = "Passw0rd!", rememberMe = false });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_WithoutCookie_Returns401AndNoLocationHeader()
    {
        HttpClient client = NewClient();

        HttpResponseMessage response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Logout_WhenAuthed_Returns204ClearsCookieAndSubsequentMeIs401()
    {
        HttpClient client = NewClient();
        string email = $"heidi-{Guid.NewGuid():N}@example.com";
        await client.PostAsJsonAsync(
            "/api/auth/register", NewRegisterBody(email, "Heidi", "Passw0rd!"));

        HttpResponseMessage logout = await client.PostAsync("/api/auth/logout", content: null);
        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);

        Assert.Contains(logout.Headers, header => header.Key == "Set-Cookie");

        HttpResponseMessage me = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, me.StatusCode);
    }
}
