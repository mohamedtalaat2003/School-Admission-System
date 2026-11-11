using SchoolAdmissionSystem.Api.Models.Dtos;

namespace SchoolAdmissionSystem.Api.Services;

public interface IClassService
{
    Task<ClassResponse> CreateClassAsync(CreateClassRequest request, CancellationToken ct = default);
    Task<IReadOnlyList<ClassSeatStatusResponse>> GetClassesAsync(int? page = null, int? pageSize = null, CancellationToken ct = default);
    Task<ClassSeatStatusResponse?> GetClassByIdAsync(string classId, CancellationToken ct = default);
}
