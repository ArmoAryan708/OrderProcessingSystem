using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OPS.Database.DbContext;

namespace OPS.IntegrationTests;

public class WebAppFactory : WebApplicationFactory<Program>
{
    // Each factory instance gets its own uniquely named in-memory database so test
    // classes running in parallel never share tables or rows.
    private readonly string _dbName = $"testdb_{Guid.NewGuid():N}";

    // The keep-alive connection prevents SQLite from destroying the in-memory database
    // when individual test scopes close their connections.
    private readonly SqliteConnection _keepAliveConnection;

    public WebAppFactory()
    {
        _keepAliveConnection = new($"DataSource={_dbName};Mode=Memory;Cache=Shared");
        _keepAliveConnection.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureServices(services =>
        {
            // ── EF Core 9 dual-provider fix ───────────────────────────────────────────
            // EF Core 9 introduced IDbContextOptionsConfiguration<T> (stored via
            // AddSingleton, not TryAdd) to hold the options action passed to AddDbContext.
            // If AddDbContext is called twice the factory iterates BOTH configurations and
            // places both provider extensions in a single DbContextOptions, which EF Core 9
            // rejects ("Multiple database providers registered").
            //
            // Remove the SQL Server DbContextOptions and every IDbContextOptionsConfiguration
            // for AppDbContext before registering our SQLite-only version.

            var dbOptionsDescriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));
            if (dbOptionsDescriptor != null)
                services.Remove(dbOptionsDescriptor);

            var optionsCfgDescriptors = services
                .Where(d =>
                    d.ServiceType.IsGenericType &&
                    d.ServiceType.GetGenericTypeDefinition().Name
                        .StartsWith("IDbContextOptionsConfiguration", StringComparison.Ordinal) &&
                    d.ServiceType.GetGenericArguments() is [{ } arg] &&
                    arg == typeof(AppDbContext))
                .ToList();
            foreach (var d in optionsCfgDescriptors)
                services.Remove(d);

            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite(_keepAliveConnection));

            // ── Logging ───────────────────────────────────────────────────────────────
            // Program.cs configures Serilog via UseSerilog(), which registers a singleton
            // ILoggerFactory that calls ReloadableLogger.Freeze() when first resolved.
            // Creating multiple WebApplicationFactory instances triggers a race condition
            // where the second factory freezes a logger that is already frozen.
            //
            // Fix: remove the Serilog-registered ILoggerFactory before it is resolved,
            // and replace it with the standard Microsoft logging factory.
            var serilogDescriptors = services
                .Where(d => d.ServiceType == typeof(ILoggerFactory))
                .ToList();
            foreach (var d in serilogDescriptors) services.Remove(d);

            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Error);
            });
        });

        // Use "Testing" so Program.cs skips MigrateAsync (which uses SQL Server DDL).
        // Tests call EnsureCreatedAsync themselves to initialise the schema on first use.
        builder.UseEnvironment("Testing");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
            _keepAliveConnection.Dispose();
        base.Dispose(disposing);
    }
}
