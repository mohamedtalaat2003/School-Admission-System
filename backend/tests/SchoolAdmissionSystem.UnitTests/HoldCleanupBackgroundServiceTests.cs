using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SchoolAdmissionSystem.Api.Data;
using SchoolAdmissionSystem.Api.Models.Entities;
using SchoolAdmissionSystem.Api.Models.Enums;
using SchoolAdmissionSystem.Api.Services;
using Xunit;

namespace SchoolAdmissionSystem.UnitTests;

public class HoldCleanupBackgroundServiceTests
{
    private static (DbContextOptions<AppDbContext> options, IServiceProvider serviceProvider) CreateServices()
    {
        var dbName = Guid.NewGuid().ToString();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: dbName)
            .Options;

        var serviceCollection = new ServiceCollection();
        serviceCollection.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase(dbName));
        return (options, serviceCollection.BuildServiceProvider());
    }

    [Fact]
    public async Task DoCleanupAsync_MarksExpiredHoldsAsExpired_LeavesActiveHoldsUntouched()
    {
        // Arrange
        var (options, serviceProvider) = CreateServices();
        var loggerMock = new Mock<ILogger<HoldCleanupBackgroundService>>();
        var service = new HoldCleanupBackgroundService(serviceProvider, loggerMock.Object);

        var now = DateTimeOffset.UtcNow;

        await using (var seedContext = new AppDbContext(options))
        {
            var expiredHold = new Hold
            {
                Id = "h_expired",
                ClassId = "c_1",
                StudentId = "11111111111111111",
                Status = HoldStatus.HELD,
                ExpiresAt = now.AddSeconds(-30),
                CreatedAt = now.AddMinutes(-5)
            };

            var activeHold = new Hold
            {
                Id = "h_active",
                ClassId = "c_1",
                StudentId = "22222222222222222",
                Status = HoldStatus.HELD,
                ExpiresAt = now.AddMinutes(5),
                CreatedAt = now
            };

            seedContext.Holds.AddRange(expiredHold, activeHold);
            await seedContext.SaveChangesAsync();
        }

        // Act
        var count = await service.DoCleanupAsync(CancellationToken.None);

        // Assert
        Assert.Equal(1, count);
        await using (var assertContext = new AppDbContext(options))
        {
            var updatedExpired = await assertContext.Holds.FindAsync("h_expired");
            var updatedActive = await assertContext.Holds.FindAsync("h_active");

            Assert.NotNull(updatedExpired);
            Assert.Equal(HoldStatus.EXPIRED, updatedExpired.Status);

            Assert.NotNull(updatedActive);
            Assert.Equal(HoldStatus.HELD, updatedActive.Status);
        }
    }

    [Fact]
    public async Task DoCleanupAsync_LeavesConfirmedAndCancelledHoldsUntouched()
    {
        // Arrange
        var (options, serviceProvider) = CreateServices();
        var loggerMock = new Mock<ILogger<HoldCleanupBackgroundService>>();
        var service = new HoldCleanupBackgroundService(serviceProvider, loggerMock.Object);

        var now = DateTimeOffset.UtcNow;

        await using (var seedContext = new AppDbContext(options))
        {
            var confirmedHold = new Hold
            {
                Id = "h_confirmed",
                ClassId = "c_1",
                StudentId = "33333333333333333",
                Status = HoldStatus.CONFIRMED,
                ExpiresAt = now.AddSeconds(-30),
                CreatedAt = now.AddMinutes(-10)
            };

            var cancelledHold = new Hold
            {
                Id = "h_cancelled",
                ClassId = "c_1",
                StudentId = "44444444444444444",
                Status = HoldStatus.CANCELLED,
                ExpiresAt = now.AddSeconds(-30),
                CreatedAt = now.AddMinutes(-10)
            };

            seedContext.Holds.AddRange(confirmedHold, cancelledHold);
            await seedContext.SaveChangesAsync();
        }

        // Act
        var count = await service.DoCleanupAsync(CancellationToken.None);

        // Assert
        Assert.Equal(0, count);
        await using (var assertContext = new AppDbContext(options))
        {
            var updatedConfirmed = await assertContext.Holds.FindAsync("h_confirmed");
            var updatedCancelled = await assertContext.Holds.FindAsync("h_cancelled");

            Assert.NotNull(updatedConfirmed);
            Assert.Equal(HoldStatus.CONFIRMED, updatedConfirmed.Status);

            Assert.NotNull(updatedCancelled);
            Assert.Equal(HoldStatus.CANCELLED, updatedCancelled.Status);
        }
    }

    [Fact]
    public async Task ExecuteAsync_StopsGracefully_WhenCancellationRequested()
    {
        // Arrange
        var (_, serviceProvider) = CreateServices();
        var loggerMock = new Mock<ILogger<HoldCleanupBackgroundService>>();
        var service = new HoldCleanupBackgroundService(serviceProvider, loggerMock.Object, TimeSpan.FromMilliseconds(50));

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // Pre-cancelled token

        // Act & Assert
        await service.StartAsync(cts.Token);
        await service.StopAsync(CancellationToken.None);
    }
}
