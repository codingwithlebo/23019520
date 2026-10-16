using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace RaceDay.Tests;

/// <summary>
/// Starts the real API in memory, with an in-memory database instead of SQL Server,
/// so tests exercise the real controllers, authentication and role checks.
/// </summary>
public class RaceDayFactory : WebApplicationFactory<Program>
{
    public const string OrganiserCode = "TEST-ORGANISER-CODE";

    private readonly string _databaseName = $"RaceDayTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");

        builder.ConfigureAppConfiguration((_, config) =>
        {
            config.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Registration:OrganiserCode"] = OrganiserCode,
                ["ConnectionStrings:RaceDayDb"] = "unused-in-tests"
            });
        });

        builder.ConfigureServices(services =>
        {
            var existing = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<RaceDayDbContext>));
            if (existing is not null) services.Remove(existing);

            services.AddDbContext<RaceDayDbContext>(options => options.UseInMemoryDatabase(_databaseName));
        });
    }
}
