using Microsoft.EntityFrameworkCore;
using SchoolAdmissionSystem.Api.Data;
using SchoolAdmissionSystem.Api.Models.Dtos;
using SchoolAdmissionSystem.Api.Models.Entities;
using SchoolAdmissionSystem.Api.Models.Enums;

namespace SchoolAdmissionSystem.Api.Services;

public class ClassService : IClassService
{
    private readonly AppDbContext _context;

    public ClassService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<ClassResponse> CreateClassAsync(CreateClassRequest request, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            throw new ArgumentException("Class name is required.", nameof(request.Name));
        }

        if (request.TotalSeats <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(request.TotalSeats), "Total seats must be greater than zero.");
        }

        var newClass = new Class
        {
            Id = $"c_{Guid.NewGuid():N}"[..10],
            Name = request.Name.Trim(),
            TotalSeats = request.TotalSeats,
            Version = 1,
            CreatedAt = DateTimeOffset.UtcNow
        };

        _context.Classes.Add(newClass);
        await _context.SaveChangesAsync(ct);

        return new ClassResponse
        {
            Id = newClass.Id,
            Name = newClass.Name,
            TotalSeats = newClass.TotalSeats
        };
    }

    public async Task<IReadOnlyList<ClassSeatStatusResponse>> GetClassesAsync(int? page = null, int? pageSize = null, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        var query = _context.Classes
            .AsNoTracking()
            .OrderBy(c => c.CreatedAt)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,
                HeldSeats = c.Holds.Count(h => h.Status == HoldStatus.HELD && h.ExpiresAt > now),
                ConfirmedSeats = c.Holds.Count(h => h.Status == HoldStatus.CONFIRMED)
            });

        if (page.HasValue && pageSize.HasValue && page.Value > 0 && pageSize.Value > 0)
        {
            query = query.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
        }

        var items = await query.ToListAsync(ct);

        return items.Select(x => new ClassSeatStatusResponse
        {
            Id = x.Id,
            Name = x.Name,
            TotalSeats = x.TotalSeats,
            HeldSeats = x.HeldSeats,
            ConfirmedSeats = x.ConfirmedSeats,
            AvailableSeats = Math.Max(0, x.TotalSeats - (x.HeldSeats + x.ConfirmedSeats))
        }).ToList();
    }

    public async Task<ClassSeatStatusResponse?> GetClassByIdAsync(string classId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        var classItem = await _context.Classes
            .AsNoTracking()
            .Where(c => c.Id == classId)
            .Select(c => new
            {
                c.Id,
                c.Name,
                c.TotalSeats,
                HeldSeats = c.Holds.Count(h => h.Status == HoldStatus.HELD && h.ExpiresAt > now),
                ConfirmedSeats = c.Holds.Count(h => h.Status == HoldStatus.CONFIRMED)
            })
            .FirstOrDefaultAsync(ct);

        if (classItem == null)
        {
            return null;
        }

        return new ClassSeatStatusResponse
        {
            Id = classItem.Id,
            Name = classItem.Name,
            TotalSeats = classItem.TotalSeats,
            HeldSeats = classItem.HeldSeats,
            ConfirmedSeats = classItem.ConfirmedSeats,
            AvailableSeats = Math.Max(0, classItem.TotalSeats - (classItem.HeldSeats + classItem.ConfirmedSeats))
        };
    }
}
