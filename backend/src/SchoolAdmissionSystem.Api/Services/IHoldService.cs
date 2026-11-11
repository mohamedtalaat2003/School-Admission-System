using SchoolAdmissionSystem.Api.Models.Dtos;

namespace SchoolAdmissionSystem.Api.Services;

public interface IHoldService
{
    Task<HoldResponse> CreateHoldAsync(string classId, string studentId, CancellationToken ct = default);
    Task<HoldStatusResponse?> GetHoldByIdAsync(string holdId, CancellationToken ct = default);
    Task<ConfirmHoldResponse> ConfirmHoldAsync(string holdId, string studentId, CancellationToken ct = default);
    Task<bool> CancelHoldAsync(string holdId, string studentId, CancellationToken ct = default);
    Task<IReadOnlyList<StudentHoldResponse>> GetMyHoldsAsync(string studentId, CancellationToken ct = default);
}
