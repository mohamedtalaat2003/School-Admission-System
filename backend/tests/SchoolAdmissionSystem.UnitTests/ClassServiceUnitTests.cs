using Microsoft.EntityFrameworkCore;
using SchoolAdmissionSystem.Api.Data;
using SchoolAdmissionSystem.Api.Models.Dtos;
using SchoolAdmissionSystem.Api.Models.Entities;
using SchoolAdmissionSystem.Api.Models.Enums;
using SchoolAdmissionSystem.Api.Services;
using Xunit;

namespace SchoolAdmissionSystem.UnitTests;

public class ClassServiceUnitTests
{
    private static AppDbContext CreateContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task CreateClassAsync_ThrowsArgumentException_WhenNameIsEmpty(string? name)
    {
        await using var context = CreateContext();
        var service = new ClassService(context);

        var request = new CreateClassRequest
        {
            Name = name!,
            TotalSeats = 30
        };

        await Assert.ThrowsAsync<ArgumentException>(() => service.CreateClassAsync(request));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-50)]
    public async Task CreateClassAsync_ThrowsArgumentOutOfRangeException_WhenSeatsLessOrEqualToZero(int seats)
    {
        await using var context = CreateContext();
        var service = new ClassService(context);

        var request = new CreateClassRequest
        {
            Name = "Class 7 - Morning",
            TotalSeats = seats
        };

        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() => service.CreateClassAsync(request));
    }

    [Fact]
    public async Task CreateClassAsync_CreatesClassSuccessfully_WithInitialVersion()
    {
        await using var context = CreateContext();
        var service = new ClassService(context);

        var request = new CreateClassRequest
        {
            Name = "Class 6 - Morning Shift",
            TotalSeats = 120
        };

        var response = await service.CreateClassAsync(request);

        Assert.NotNull(response);
        Assert.StartsWith("c_", response.Id);
        Assert.Equal("Class 6 - Morning Shift", response.Name);
        Assert.Equal(120, response.TotalSeats);

        var created = await context.Classes.FindAsync(response.Id);
        Assert.NotNull(created);
        Assert.Equal(1, created.Version);
    }

    [Fact]
    public async Task GetClassesAsync_ReturnsClassesWithCorrectAvailableSeats()
    {
        await using var context = CreateContext();
        var service = new ClassService(context);

        var newClass = new Class
        {
            Id = "c_1",
            Name = "Class 8 - Day",
            TotalSeats = 50,
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };
        context.Classes.Add(newClass);

        var now = DateTimeOffset.UtcNow;
        var activeHold = new Hold
        {
            Id = "h_1",
            ClassId = "c_1",
            StudentId = "11111111111111111",
            Status = HoldStatus.HELD,
            ExpiresAt = now.AddMinutes(5),
            CreatedAt = now
        };
        var confirmedHold = new Hold
        {
            Id = "h_2",
            ClassId = "c_1",
            StudentId = "22222222222222222",
            Status = HoldStatus.CONFIRMED,
            ExpiresAt = now.AddMinutes(5),
            CreatedAt = now
        };
        var expiredHold = new Hold
        {
            Id = "h_3",
            ClassId = "c_1",
            StudentId = "33333333333333333",
            Status = HoldStatus.HELD,
            ExpiresAt = now.AddMinutes(-5),
            CreatedAt = now.AddMinutes(-10)
        };

        context.Holds.AddRange(activeHold, confirmedHold, expiredHold);
        await context.SaveChangesAsync();

        var result = await service.GetClassesAsync();

        Assert.Single(result);
        var item = result[0];
        Assert.Equal(50, item.TotalSeats);
        Assert.Equal(1, item.HeldSeats);       // only active hold counts
        Assert.Equal(1, item.ConfirmedSeats);  // confirmed hold counts
        Assert.Equal(48, item.AvailableSeats); // 50 - (1 + 1)
    }

    [Fact]
    public async Task GetClassByIdAsync_ReturnsNull_WhenClassDoesNotExist()
    {
        await using var context = CreateContext();
        var service = new ClassService(context);

        var result = await service.GetClassByIdAsync("c_nonexistent");

        Assert.Null(result);
    }
}
