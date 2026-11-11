using System.Data;
using Microsoft.EntityFrameworkCore;
using SchoolAdmissionSystem.Api.Data;
using SchoolAdmissionSystem.Api.Models.Dtos;
using SchoolAdmissionSystem.Api.Models.Entities;
using SchoolAdmissionSystem.Api.Models.Enums;

namespace SchoolAdmissionSystem.Api.Services;

public class HoldService : IHoldService
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<HoldService> _logger;

    public HoldService(AppDbContext context, IConfiguration configuration, ILogger<HoldService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<HoldResponse> CreateHoldAsync(string classId, string studentId, CancellationToken ct = default)
    {
        var holdDurationSeconds = _configuration.GetValue("HOLD_DURATION_SECONDS", 600);
        const int maxRetries = 3;

        for (var attempt = 1; attempt <= maxRetries; attempt++)
        {
            await using var transaction = await _context.Database.BeginTransactionAsync(IsolationLevel.ReadCommitted, ct);
            try
            {
                var now = DateTimeOffset.UtcNow;

                var classEntity = await _context.Classes.FirstOrDefaultAsync(c => c.Id == classId, ct);
                if (classEntity == null)
                {
                    throw new KeyNotFoundException($"Class with id '{classId}' does not exist.");
                }

                // Check duplicate active or confirmed application for this class
                var hasActiveOrConfirmed = await _context.Holds.AnyAsync(h =>
                    h.ClassId == classId &&
                    h.StudentId == studentId &&
                    (h.Status == HoldStatus.CONFIRMED || (h.Status == HoldStatus.HELD && h.ExpiresAt > now)), ct);

                if (hasActiveOrConfirmed)
                {
                    throw new InvalidOperationException("Student already has an active application or confirmed admission for this class.");
                }

                // Capacity check
                var activeCount = await _context.Holds.CountAsync(h =>
                    h.ClassId == classId &&
                    (h.Status == HoldStatus.CONFIRMED || (h.Status == HoldStatus.HELD && h.ExpiresAt > now)), ct);

                if (activeCount >= classEntity.TotalSeats)
                {
                    throw new InvalidOperationException("No seats available for this class.");
                }

                var holdId = $"h_{Guid.NewGuid():N}"[..10];
                var expiresAt = now.AddSeconds(holdDurationSeconds);

                var newHold = new Hold
                {
                    Id = holdId,
                    ClassId = classId,
                    StudentId = studentId,
                    Status = HoldStatus.HELD,
                    ExpiresAt = expiresAt,
                    CreatedAt = now
                };

                // Modify version token on Class for optimistic concurrency check
                classEntity.Version++;
                _context.Holds.Add(newHold);

                await _context.SaveChangesAsync(ct);
                await transaction.CommitAsync(ct);

                return new HoldResponse
                {
                    HoldId = newHold.Id,
                    ClassId = newHold.ClassId,
                    StudentId = newHold.StudentId,
                    Status = newHold.Status.ToString(),
                    ExpiresAt = newHold.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
                };
            }
            catch (DbUpdateConcurrencyException)
            {
                await transaction.RollbackAsync(ct);
                _context.ChangeTracker.Clear();

                if (attempt == maxRetries)
                {
                    _logger.LogWarning("Seat reservation exhausted retries due to concurrency for class {ClassId}", classId);
                    throw new InvalidOperationException("No seats available for this class due to concurrent reservations.");
                }

                await Task.Delay(15 * attempt, ct);
            }
            catch
            {
                await transaction.RollbackAsync(ct);
                _context.ChangeTracker.Clear();
                throw;
            }
        }

        throw new InvalidOperationException("No seats available for this class.");
    }

    public async Task<HoldStatusResponse?> GetHoldByIdAsync(string holdId, CancellationToken ct = default)
    {
        var hold = await _context.Holds.AsNoTracking().FirstOrDefaultAsync(h => h.Id == holdId, ct);
        if (hold == null)
        {
            return null;
        }

        var now = DateTimeOffset.UtcNow;
        var status = hold.Status;
        if (status == HoldStatus.HELD && hold.ExpiresAt <= now)
        {
            status = HoldStatus.EXPIRED;
        }

        return new HoldStatusResponse
        {
            HoldId = hold.Id,
            ClassId = hold.ClassId,
            Status = status.ToString(),
            ExpiresAt = hold.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
        };
    }

    public async Task<ConfirmHoldResponse> ConfirmHoldAsync(string holdId, string studentId, CancellationToken ct = default)
    {
        var hold = await _context.Holds.FirstOrDefaultAsync(h => h.Id == holdId, ct);
        if (hold == null)
        {
            throw new KeyNotFoundException($"Application with id '{holdId}' was not found.");
        }

        if (hold.StudentId != studentId)
        {
            throw new UnauthorizedAccessException("You do not have permission to confirm this application.");
        }

        // Idempotency: repeated confirmations succeed without incrementing seats
        if (hold.Status == HoldStatus.CONFIRMED)
        {
            return new ConfirmHoldResponse
            {
                HoldId = hold.Id,
                Status = "CONFIRMED"
            };
        }

        var now = DateTimeOffset.UtcNow;
        if (hold.Status == HoldStatus.CANCELLED)
        {
            throw new InvalidOperationException("Cannot confirm a cancelled application.");
        }

        if (hold.ExpiresAt <= now || hold.Status == HoldStatus.EXPIRED)
        {
            throw new InvalidOperationException("Application hold has expired.");
        }

        hold.Status = HoldStatus.CONFIRMED;
        hold.ConfirmedAt = now;
        await _context.SaveChangesAsync(ct);

        return new ConfirmHoldResponse
        {
            HoldId = hold.Id,
            Status = "CONFIRMED"
        };
    }

    public async Task<bool> CancelHoldAsync(string holdId, string studentId, CancellationToken ct = default)
    {
        var hold = await _context.Holds.FirstOrDefaultAsync(h => h.Id == holdId, ct);
        if (hold == null)
        {
            throw new KeyNotFoundException($"Application with id '{holdId}' was not found.");
        }

        if (hold.StudentId != studentId)
        {
            throw new UnauthorizedAccessException("You do not have permission to cancel this application.");
        }

        if (hold.Status == HoldStatus.CONFIRMED)
        {
            throw new InvalidOperationException("Confirmed admissions cannot be cancelled.");
        }

        if (hold.Status == HoldStatus.HELD)
        {
            hold.Status = HoldStatus.CANCELLED;
            await _context.SaveChangesAsync(ct);
        }

        return true;
    }

    public async Task<IReadOnlyList<StudentHoldResponse>> GetMyHoldsAsync(string studentId, CancellationToken ct = default)
    {
        var now = DateTimeOffset.UtcNow;

        var holds = await _context.Holds
            .AsNoTracking()
            .Where(h => h.StudentId == studentId)
            .Include(h => h.Class)
            .OrderByDescending(h => h.CreatedAt)
            .ToListAsync(ct);

        return holds.Select(h =>
        {
            var resolvedStatus = h.Status;
            if (resolvedStatus == HoldStatus.HELD && h.ExpiresAt <= now)
            {
                resolvedStatus = HoldStatus.EXPIRED;
            }

            return new StudentHoldResponse
            {
                HoldId = h.Id,
                ClassId = h.ClassId,
                ClassName = h.Class?.Name ?? string.Empty,
                Status = resolvedStatus.ToString(),
                ExpiresAt = h.ExpiresAt.ToString("yyyy-MM-ddTHH:mm:ssZ")
            };
        }).ToList();
    }
}
