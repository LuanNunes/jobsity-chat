using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace com.jobsite.chat.Tests.Api.Integration;

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

    [Fact]
    public async Task GetRooms_WithoutCookie_Returns401AndNoLocationHeader()
    {
        HttpClient client = NewClient();

        HttpResponseMessage response = await client.GetAsync("/api/rooms");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task GetRooms_WhenAuthed_Returns200Array()
    {
        HttpClient client = await NewAuthedClientAsync();

        HttpResponseMessage response = await client.GetAsync("/api/rooms");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        JsonElement body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(JsonValueKind.Array, body.ValueKind);
    }

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

    [Fact]
    public async Task CreateRoom_WhenAuthedWithEmptyName_Returns400()
    {
        HttpClient client = await NewAuthedClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/rooms", new { name = "" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateRoom_WhenAuthedWithWhitespaceName_Returns400()
    {
        HttpClient client = await NewAuthedClientAsync();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/rooms", new { name = "   " });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateRoom_WhenAuthedWithTooLongName_Returns400()
    {
        HttpClient client = await NewAuthedClientAsync();
        string tooLong = new('x', 101);

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/rooms", new { name = tooLong });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateRoom_WithoutCookie_Returns401AndNoLocationHeader()
    {
        HttpClient client = NewClient();

        HttpResponseMessage response = await client.PostAsJsonAsync("/api/rooms", new { name = "Nope" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }
}
