using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using com.jobsite.chat.Api.Features.Chat;
using com.jobsite.chat.Domain.Dtos;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using NSubstitute;

namespace com.jobsite.chat.Tests.Api.Integration;

// Spec (plan mellow-toasting-engelbart): creating a room over REST must broadcast a `RoomCreated`
// hub event to ALL connected clients so other signed-in SPAs update their sidebar without a refresh.
//
// This tests the REAL behavior — "POST /api/rooms broadcasts RoomCreated to Clients.All" — through
// the actual endpoint + real DI + real auth cookie, but replaces the strongly-typed hub context
// (`IHubContext<ChatHub, IChatClient>`) with an NSubstitute mock. That deliberately avoids the
// in-memory SignalR socket transport, whose cookie-over-TestServer handshake the sibling
// SignalRHubTests documents as plumbing-flaky. The seam here is deterministic and always-run.
//
// RED today: `IChatClient` has no `RoomCreated` method and the endpoint does not broadcast, so this
// file will not compile until the implementer adds `Task RoomCreated(ChatRoomDto room);` to
// IChatClient. Once compiling, `Received(1)` guards that the endpoint actually calls it.
public sealed class RoomBroadcastTests
{
    // Standalone factory (the shared ApiWebApplicationFactory is sealed): mirrors RateLimitingTests'
    // SmallAuthLimitFactory isolation — unique throwaway SQLite db per instance, Development env,
    // pinned SPA CORS origin — and additionally swaps the strongly-typed hub context for a
    // substitute via ConfigureTestServices (which runs AFTER the app's own registrations, so the
    // RemoveAll + AddSingleton override wins).
    private sealed class RoomBroadcastFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath =
            Path.Combine(Path.GetTempPath(), $"jobsity-chat-broadcast-test-{Guid.NewGuid():N}.db");

        // Built up front (field initializers run before ConfigureWebHost) so DI hands the endpoint
        // the SAME instances the test asserts on.
        public IChatClient AllClientSubstitute { get; } = Substitute.For<IChatClient>();

        private readonly IHubContext<ChatHub, IChatClient> _hubContext =
            Substitute.For<IHubContext<ChatHub, IChatClient>>();

        private readonly IHubClients<IChatClient> _clients = Substitute.For<IHubClients<IChatClient>>();

        public RoomBroadcastFactory()
        {
            _clients.All.Returns(AllClientSubstitute);
            _hubContext.Clients.Returns(_clients);
        }

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.UseEnvironment(Environments.Development);

            builder.ConfigureAppConfiguration((context, configuration) =>
            {
                Dictionary<string, string?> overrides = new()
                {
                    ["ConnectionStrings:ChatDatabase"] = $"Data Source={_dbPath}",
                    ["Cors:AllowedOrigin"] = "http://localhost:3000",
                };
                configuration.AddInMemoryCollection(overrides);
            });

            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IHubContext<ChatHub, IChatClient>>();
                services.AddSingleton(_hubContext);
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

    private static HttpClient NewClient(RoomBroadcastFactory factory) =>
        factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

    // Creating a room while authenticated broadcasts RoomCreated to every connected client
    // (Clients.All), carrying the created room's name and a real id.
    [Fact]
    public async Task CreateRoom_WhenAuthed_BroadcastsRoomCreatedToAllClients()
    {
        using RoomBroadcastFactory factory = new();
        HttpClient client = NewClient(factory);

        // Register (returns the auth cookie in the client's cookie container) so POST /api/rooms
        // — which RequireAuthorization — is accepted.
        string email = $"broadcaster-{Guid.NewGuid():N}@example.com";
        HttpResponseMessage register = await client.PostAsJsonAsync(
            "/api/auth/register", new { email, displayName = "Broadcaster", password = "Passw0rd!" });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        string roomName = $"Broadcasts-{Guid.NewGuid():N}";
        HttpResponseMessage created = await client.PostAsJsonAsync("/api/rooms", new { name = roomName });

        Assert.Equal(HttpStatusCode.Created, created.StatusCode);
        await factory.AllClientSubstitute.Received(1)
            .RoomCreated(Arg.Is<ChatRoomDto>(room => room.Name == roomName && room.Id != Guid.Empty));
    }
}
