using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

namespace com.jobsite.chat.Tests.Api.Integration;

// Boots the real Api host (Program) for integration tests while isolating it from the developer
// database. CRITICAL (spec §7.2 / task): the app runs migrate-on-startup against
// ConnectionStrings:ChatDatabase, so we override that key to a UNIQUE throwaway SQLite file per
// factory instance and delete it (plus WAL/SHM sidecars) on Dispose. The dev jobsity-chat.db is
// never touched. We also pin Cors:AllowedOrigin and force the Development environment so the host
// does not enable HTTPS redirection/HSTS (which would break the cross-origin cookie flow the SPA
// depends on and the tests assert against).
public sealed class ApiWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"jobsity-chat-test-{Guid.NewGuid():N}.db");

    public const string SpaOrigin = "http://localhost:3000";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment(Environments.Development);

        builder.ConfigureAppConfiguration((context, configuration) =>
        {
            Dictionary<string, string?> overrides = new()
            {
                ["ConnectionStrings:ChatDatabase"] = $"Data Source={_dbPath}",
                ["Cors:AllowedOrigin"] = SpaOrigin,
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
                    // Best-effort cleanup of the throwaway db; a locked sidecar must not fail tests.
                }
            }
        }
    }
}
