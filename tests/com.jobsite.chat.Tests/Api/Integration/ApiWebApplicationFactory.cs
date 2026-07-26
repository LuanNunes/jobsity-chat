using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace com.jobsite.chat.Tests.Api.Integration;

// Boots the real Api host (Program) for integration tests while isolating it from the developer
// database. CRITICAL (spec §7.2 / task): the app runs migrate-on-startup against
// ConnectionStrings:ChatDatabase, so we override that key to a UNIQUE throwaway SQLite file per
// factory instance and delete it (plus WAL/SHM sidecars) on Dispose. The dev jobsity-chat.db is
// never touched. We force the Development environment so the host skips HTTPS redirection/HSTS
// (which would break the cross-origin cookie flow the tests assert) and so it picks up the
// test-friendly rate-limit ceilings in appsettings.Development.json. The allowed SPA origin comes
// from configuration (Cors:AllowedOrigin) — exposed via SpaOrigin.
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    // SQLite writes sidecar files next to the db: -wal/-shm (WAL mode) or -journal (rollback mode);
    // string.Empty is the main db file. Used to sweep the throwaway db on Dispose.
    private static readonly string[] SqliteFileSuffixes = [string.Empty, "-wal", "-shm", "-journal"];

    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"jobsity-chat-test-{Guid.NewGuid():N}.db");

    // The CORS-allowed SPA origin, sourced from configuration rather than hardcoded here.
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
                // Best-effort cleanup of the throwaway db; a locked sidecar must not fail tests.
            }
        }
    }
}
