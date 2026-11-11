using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
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
        builder.UseEnvironment("Testing");
        builder.UseSetting("HOLD_DURATION_SECONDS", _holdDurationSeconds.ToString());

        builder.ConfigureServices(services =>
        {
            services.AddDbContext<AppDbContext>(options =>
            {
                options.UseInMemoryDatabase(_databaseName);
                options.ConfigureWarnings(w => w.Ignore(InMemoryEventId.TransactionIgnoredWarning));
            });
        });
    }
}
