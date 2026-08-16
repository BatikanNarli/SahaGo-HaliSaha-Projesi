using Halisaha.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Halisaha.IntegrationTests;

internal sealed class TestApiFactory : WebApplicationFactory<Program>
{
    private readonly string databaseName = $"HalisahaTests-{Guid.NewGuid()}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration((_, configuration) =>
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Halisaha"] =
                    "Server=localhost;Database=HalisahaTest;Integrated Security=True;TrustServerCertificate=True",
                ["Authentication:Jwt:SigningKey"] =
                    "integration-test-signing-key-that-is-longer-than-thirty-two-bytes",
                ["DevelopmentData:SeedSampleCatalog"] = "false",
                ["Logging:LogLevel:Microsoft.AspNetCore.Authentication"] = "Debug"
            }));
        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<HalisahaDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<HalisahaDbContext>>();
            services.AddDbContext<HalisahaDbContext>(options => options.UseInMemoryDatabase(databaseName));
        });
    }

    protected override IHost CreateHost(IHostBuilder builder)
    {
        var host = base.CreateHost(builder);
        using var scope = host.Services.CreateScope();
        scope.ServiceProvider.GetRequiredService<HalisahaDbContext>().Database.EnsureCreated();
        scope.ServiceProvider.GetRequiredService<DevelopmentDataSeeder>()
            .SeedAsync(CancellationToken.None)
            .GetAwaiter()
            .GetResult();
        return host;
    }
}
