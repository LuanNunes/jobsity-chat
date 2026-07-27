using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace com.jobsite.chat.Tests.Api.Integration;

public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{

    private static readonly string[] SqliteFileSuffixes = [string.Empty, "-wal", "-shm", "-journal"];

    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"jobsity-chat-test-{Guid.NewGuid():N}.db");

    public string SpaOrigin => Services.GetRequiredService<IConfiguration>()["Cors:AllowedOrigin"]!;

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((context, configuration) =>
        {
            Dictionary<string, string?> overrides = new()
            {
                ["ConnectionStrings:ChatDatabase"] = $"Data Source={_dbPath}",
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

        foreach (string suffix in SqliteFileSuffixes)
        {
            string path = _dbPath + suffix;
            if (!File.Exists(path))
            {
                continue;
            }

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
