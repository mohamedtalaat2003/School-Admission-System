using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SchoolAdmissionSystem.Api.Data;

namespace SchoolAdmissionSystem.IntegrationTests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private readonly string _databaseName;
    private readonly int _holdDurationSeconds;

    public CustomWebApplicationFactory(string? databaseName = null, int holdDurationSeconds = 600)
    {
        _databaseName = databaseName ?? Guid.NewGuid().ToString();
        _holdDurationSeconds = holdDurationSeconds;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("HOLD_DURATION_SECONDS", _holdDurationSeconds.ToString());

        builder.ConfigureServices(services =>
        {
            // Remove existing DbContext registration
            var descriptor = services.SingleOrDefault(
                d => d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Register in-memory DbContext with unique database name per test
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
            });
        });
    }
}
