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

public sealed class RoomBroadcastTests
{

    private sealed class RoomBroadcastFactory : WebApplicationFactory<Program>
    {
        private readonly string _dbPath =
            Path.Combine(Path.GetTempPath(), $"jobsity-chat-broadcast-test-{Guid.NewGuid():N}.db");

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

    [Fact]
    public async Task CreateRoom_WhenAuthed_BroadcastsRoomCreatedToAllClients()
    {
        using RoomBroadcastFactory factory = new();
        HttpClient client = NewClient(factory);

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
