using System.Text.Json;
using System.Text.Json.Serialization;
using AdvancedPCBuilder.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace AdvancedPCBuilder.Api.Tests;

/// <summary>
/// Starts the real API in memory (same Program.cs, same DI, same middleware) for integration tests.
/// Each factory gets its OWN SQLite file in the temp folder, so test classes never share data and
/// the developer's advancedpcbuilder.db is never touched. The API creates and seeds the file on
/// startup exactly as in production; the file is deleted when the factory is disposed.
/// Used as an xUnit class fixture: one factory (one database) per test class.
/// </summary>
public sealed class ApiFactory : WebApplicationFactory<Program>
{
    public const string TechShop = SeedData.TechShopId;
    public const string PcMeistrai = SeedData.PcMeistraiId;

    /// <summary>Same JSON settings as the API: camelCase and enums as strings.</summary>
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    private readonly string _databasePath =
        Path.Combine(Path.GetTempPath(), $"advancedpcbuilder-test-{Guid.NewGuid():N}.db");

    private string ConnectionString => $"Data Source={_databasePath}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Overrides ConnectionStrings:Default from appsettings.json before Program.cs reads it.
        builder.UseSetting("ConnectionStrings:Default", ConnectionString);
    }

    /// <summary>An HTTP client that sends the X-Tenant-Id header on every request.</summary>
    public HttpClient CreateTenantClient(string tenantId)
    {
        HttpClient client = CreateClient();
        client.DefaultRequestHeaders.Add("X-Tenant-Id", tenantId);
        return client;
    }

    /// <summary>
    /// A DbContext for one tenant on this factory's database (already created and seeded by the API).
    /// The caller disposes it.
    /// </summary>
    public AppDbContext CreateDbContext(string tenantId)
    {
        _ = Server;   // starts the API once (if not started yet), which creates and seeds the database

        DbContextOptions<AppDbContext> options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(ConnectionString)
            .Options;
        return new AppDbContext(options, new FixedTenantContext(tenantId));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        // Pooled SQLite connections keep the file open (Windows would refuse to delete it).
        SqliteConnection.ClearAllPools();
        File.Delete(_databasePath);
    }
}
