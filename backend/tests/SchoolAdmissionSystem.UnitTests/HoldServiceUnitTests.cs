using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using SchoolAdmissionSystem.Api.Data;
using SchoolAdmissionSystem.Api.Exceptions;
using SchoolAdmissionSystem.Api.Models.Entities;
using SchoolAdmissionSystem.Api.Models.Enums;
using SchoolAdmissionSystem.Api.Services;
using Xunit;

namespace SchoolAdmissionSystem.UnitTests;

public class HoldServiceUnitTests
{
    private static (AppDbContext context, IConfiguration config, ILogger<HoldService> logger) CreateDependencies()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        var context = new AppDbContext(options);

        var configMock = new Mock<IConfiguration>();
        var configSectionMock = new Mock<IConfigurationSection>();
        configSectionMock.Setup(s => s.Value).Returns("600");
        configMock.Setup(c => c.GetSection("HOLD_DURATION_SECONDS")).Returns(configSectionMock.Object);

        var loggerMock = new Mock<ILogger<HoldService>>();

        return (context, configMock.Object, loggerMock.Object);
    }

    [Fact]
    public async Task GetHoldByIdAsync_ReturnsDynamicExpired_WhenHoldTimePassed()
    {
        var (context, config, logger) = CreateDependencies();
        var service = new HoldService(context, config, logger);

        var now = DateTimeOffset.UtcNow;
        var expiredHold = new Hold
        {
            Id = "h_10",
            ClassId = "c_1",
            StudentId = "11111111111111111",
            Status = HoldStatus.HELD,
            ExpiresAt = now.AddSeconds(-10),
            CreatedAt = now.AddMinutes(-5)
        };
        context.Holds.Add(expiredHold);
        await context.SaveChangesAsync();

        var result = await service.GetHoldByIdAsync("h_10");

        Assert.NotNull(result);
        Assert.Equal("EXPIRED", result.Status);
    }

    [Fact]
    public async Task ConfirmHoldAsync_ReturnsConfirmed_WhenAlreadyConfirmed_Idempotent()
    {
        var (context, config, logger) = CreateDependencies();
        var service = new HoldService(context, config, logger);

        var now = DateTimeOffset.UtcNow;
        var confirmedHold = new Hold
        {
            Id = "h_confirmed",
            ClassId = "c_1",
            StudentId = "11111111111111111",
            Status = HoldStatus.CONFIRMED,
            ExpiresAt = now.AddMinutes(5),
            CreatedAt = now.AddMinutes(-1),
            ConfirmedAt = now.AddSeconds(-30)
        };
        context.Holds.Add(confirmedHold);
        await context.SaveChangesAsync();

        var result = await service.ConfirmHoldAsync("h_confirmed", "11111111111111111");

        Assert.NotNull(result);
        Assert.Equal("CONFIRMED", result.Status);
        Assert.Equal("h_confirmed", result.HoldId);
    }

    [Fact]
    public async Task ConfirmHoldAsync_ThrowsUnauthorized_WhenStudentDoesNotMatch()
    {
        var (context, config, logger) = CreateDependencies();
        var service = new HoldService(context, config, logger);

        var now = DateTimeOffset.UtcNow;
        var hold = new Hold
        {
            Id = "h_owner",
            ClassId = "c_1",
            StudentId = "11111111111111111",
            Status = HoldStatus.HELD,
            ExpiresAt = now.AddMinutes(5),
            CreatedAt = now
        };
        context.Holds.Add(hold);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.ConfirmHoldAsync("h_owner", "99999999999999999"));
    }

    [Fact]
    public async Task ConfirmHoldAsync_ThrowsInvalidOperation_WhenHoldExpired()
    {
        var (context, config, logger) = CreateDependencies();
        var service = new HoldService(context, config, logger);

        var now = DateTimeOffset.UtcNow;
        var expiredHold = new Hold
        {
            Id = "h_expired",
            ClassId = "c_1",
            StudentId = "11111111111111111",
            Status = HoldStatus.HELD,
            ExpiresAt = now.AddSeconds(-20),
            CreatedAt = now.AddMinutes(-5)
        };
        context.Holds.Add(expiredHold);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConfirmHoldAsync("h_expired", "11111111111111111"));
    }

    [Fact]
    public async Task ConfirmHoldAsync_ThrowsInvalidOperation_WhenHoldCancelled()
    {
        var (context, config, logger) = CreateDependencies();
        var service = new HoldService(context, config, logger);

        var now = DateTimeOffset.UtcNow;
        var cancelledHold = new Hold
        {
            Id = "h_cancelled",
            ClassId = "c_1",
            StudentId = "11111111111111111",
            Status = HoldStatus.CANCELLED,
            ExpiresAt = now.AddMinutes(5),
            CreatedAt = now
        };
        context.Holds.Add(cancelledHold);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.ConfirmHoldAsync("h_cancelled", "11111111111111111"));
    }

    [Fact]
    public async Task CancelHoldAsync_ThrowsUnauthorized_WhenStudentDoesNotMatch()
    {
        var (context, config, logger) = CreateDependencies();
        var service = new HoldService(context, config, logger);

        var now = DateTimeOffset.UtcNow;
        var hold = new Hold
        {
            Id = "h_cancel_auth",
            ClassId = "c_1",
            StudentId = "11111111111111111",
            Status = HoldStatus.HELD,
            ExpiresAt = now.AddMinutes(5),
            CreatedAt = now
        };
        context.Holds.Add(hold);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            service.CancelHoldAsync("h_cancel_auth", "99999999999999999"));
    }

    [Fact]
    public async Task CancelHoldAsync_ThrowsInvalidOperation_WhenAlreadyConfirmed()
    {
        var (context, config, logger) = CreateDependencies();
        var service = new HoldService(context, config, logger);

        var now = DateTimeOffset.UtcNow;
        var confirmedHold = new Hold
        {
            Id = "h_cancel_confirmed",
            ClassId = "c_1",
            StudentId = "11111111111111111",
            Status = HoldStatus.CONFIRMED,
            ExpiresAt = now.AddMinutes(5),
            CreatedAt = now
        };
        context.Holds.Add(confirmedHold);
        await context.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            service.CancelHoldAsync("h_cancel_confirmed", "11111111111111111"));
    }

    [Fact]
    public async Task CancelHoldAsync_SetsStatusToCancelled_WhenHeld()
    {
        var (context, config, logger) = CreateDependencies();
        var service = new HoldService(context, config, logger);

        var now = DateTimeOffset.UtcNow;
        var hold = new Hold
        {
            Id = "h_to_cancel",
            ClassId = "c_1",
            StudentId = "11111111111111111",
            Status = HoldStatus.HELD,
            ExpiresAt = now.AddMinutes(5),
            CreatedAt = now
        };
        context.Holds.Add(hold);
        await context.SaveChangesAsync();

        var success = await service.CancelHoldAsync("h_to_cancel", "11111111111111111");

        Assert.True(success);
        var updated = await context.Holds.FindAsync("h_to_cancel");
        Assert.NotNull(updated);
        Assert.Equal(HoldStatus.CANCELLED, updated.Status);
    }

    [Fact]
    public async Task GetMyHoldsAsync_ReturnsHoldsForSpecificStudent_WithDynamicStatuses()
    {
        var (context, config, logger) = CreateDependencies();
        var service = new HoldService(context, config, logger);

        var testClass = new Class
        {
            Id = "c_class1",
            Name = "Class 6 - Morning Shift",
            TotalSeats = 100,
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Classes.Add(testClass);

        var now = DateTimeOffset.UtcNow;
        var student1Hold = new Hold
        {
            Id = "h_s1",
            ClassId = "c_class1",
            StudentId = "11111111111111111",
            Status = HoldStatus.HELD,
            ExpiresAt = now.AddMinutes(5),
            CreatedAt = now
        };
        var student2Hold = new Hold
        {
            Id = "h_s2",
            ClassId = "c_class1",
            StudentId = "22222222222222222",
            Status = HoldStatus.HELD,
            ExpiresAt = now.AddMinutes(5),
            CreatedAt = now
        };
        context.Holds.AddRange(student1Hold, student2Hold);
        await context.SaveChangesAsync();

        var result = await service.GetMyHoldsAsync("11111111111111111");

        Assert.Single(result);
        Assert.Equal("h_s1", result[0].HoldId);
        Assert.Equal("Class 6 - Morning Shift", result[0].ClassName);
        Assert.Equal("HELD", result[0].Status);
    }
}
