using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;

namespace PollApp.Api.Tests.Integration;

/// <summary>
/// Custom factory that spins up the real ASP.NET Core pipeline with a per-run
/// SQLite file instead of the production polls.db.  FluentMigrator runs on
/// startup so every test-run begins with a clean, fully-migrated schema.
/// </summary>
public sealed class PollApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    // Each factory instance gets its own temp database so tests don't collide.
    private readonly string _dbPath =
        Path.Combine(Path.GetTempPath(), $"polltest-{Guid.NewGuid()}.db");

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Override the connection string BEFORE the app's Program.cs reads it,
        // so both FluentMigrator and all Repositories use the test database.
        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = $"Data Source={_dbPath};Cache=Shared"
            });
        });
    }

    public Task InitializeAsync() => Task.CompletedTask;

    public new async Task DisposeAsync()
    {
        await base.DisposeAsync();
        // Release all pooled SQLite connections so the temp file can be deleted.
        SqliteConnection.ClearAllPools();
        if (File.Exists(_dbPath))
            File.Delete(_dbPath);
    }
}
