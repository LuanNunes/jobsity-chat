using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace com.jobsite.chat.Tests.Api.Integration;

// Integration specs for the JSON rooms endpoints (spec §2.4, §7.2). RED today: the Razor host has
// no /api/rooms endpoints. Includes the pivot's headline behavior: unauthenticated access is 401
// JSON, NEVER a 302 redirect (no Location header) — the SPA relies on this.
public sealed class RoomEndpointsTests : IClassFixture<ApiWebApplicationFactory>
{
    private readonly ApiWebApplicationFactory _factory;

    public RoomEndpointsTests(ApiWebApplicationFactory factory)
    {
        _factory = factory;
    }

    private HttpClient NewClient() =>
        _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

    private async Task<HttpClient> NewAuthedClientAsync()
    {
        HttpClient client = NewClient();
        string email = $"user-{Guid.NewGuid():N}@example.com";
        HttpResponseMessage register = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, displayName = "Room User", password = "Passw0rd!" });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);
        return client;
    }

    // §7.2 #6 (HEADLINE): unauthenticated GET /api/rooms -> 401 and NO Location header.
    [Fact]
    public async Task GetRooms_WithoutCookie_Returns401AndNoLocationHeader()
    {
        HttpClient client = NewClient();

        HttpResponseMessage response = await client.GetAsync("/api/rooms");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    // §7.2 #10: authed GET /api/rooms -> 200 JSON array.
    [Fact]
    public async Task GetRooms_WhenAuthed_Returns200Array()
    {
        HttpClient client = await NewAuthedClientAsync();

        HttpResponseMessage response = await client.GetAsync("/api/rooms");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
    }

    // §7.2 #10: authed POST /api/rooms valid -> 201 ChatRoomDto (+ Location); then it shows in the list.
    [Fact]
    public async Task CreateRoom_WhenAuthedWithValidName_Returns201WithChatRoomDtoAndAppearsInList()
    {
        HttpClient client = await NewAuthedClientAsync();
        string name = $"General-{Guid.NewGuid():N}";

        HttpResponseMessage created = await client.PostAsJsonAsync("/api/rooms", new { name });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        JsonElement room = await created.Content.ReadFromJsonAsync<JsonElement>();
        Assert.False(string.IsNullOrWhiteSpace(room.GetProperty("id").GetString()));
        Assert.Equal(name, room.GetProperty("name").GetString());
        Assert.NotNull(created.Headers.Location);

        HttpResponseMessage list = await client.GetAsync("/api/rooms");
        string listJson = await list.Content.ReadAsStringAsync();
        Assert.Contains(name, listJson);
    }

    // §7.2 #11: authed POST /api/rooms with empty name -> 400 (DomainException path via the filter).
    [Fact]
    public async Task CreateRoom_WhenAuthedWithEmptyName_Returns400()
    {
        HttpClient client = await NewAuthedClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/rooms", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // §7.2 #11: authed POST /api/rooms with whitespace-only name -> 400 (trim then NotEmpty rule).
    [Fact]
    public async Task CreateRoom_WhenAuthedWithWhitespaceName_Returns400()
    {
        HttpClient client = await NewAuthedClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/rooms", new { name = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // §7.2 #11: authed POST /api/rooms with an over-long name (>100) -> 400 (MaximumLength rule).
    [Fact]
    public async Task CreateRoom_WhenAuthedWithTooLongName_Returns400()
    {
        HttpClient client = await NewAuthedClientAsync();
        string tooLong = new('x', 101);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/rooms", new { name = tooLong });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // §7.2 #6 companion: unauthenticated POST /api/rooms -> 401, no Location header.
    [Fact]
    public async Task CreateRoom_WithoutCookie_Returns401AndNoLocationHeader()
    {
        HttpClient client = NewClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/rooms", new { name = "Nope" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }
}
