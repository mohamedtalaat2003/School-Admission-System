using Microsoft.AspNetCore.Mvc;
using SchoolAdmissionSystem.Api.Filters;
using SchoolAdmissionSystem.Api.Models.Dtos;
using SchoolAdmissionSystem.Api.Services;

namespace SchoolAdmissionSystem.Api.Controllers;

[ApiController]
[Route("me")]
[Produces("application/json")]
public class MeController : ControllerBase
{
    private readonly IHoldService _holdService;

    public MeController(IHoldService holdService)
    {
        _holdService = holdService;
    }

    /// <summary>
    /// GET /me/holds - List all applications belonging to this student.
    /// </summary>
    [HttpGet("holds")]
    [RequireStudentId]
    [ProducesResponseType(typeof(IReadOnlyList<StudentHoldResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Holds(CancellationToken ct)
    {
        var studentId = (string)HttpContext.Items[RequireStudentIdAttribute.HeaderName]!;
        var holds = await _holdService.GetMyHoldsAsync(studentId.Trim(), ct);
        return Ok(holds);
    }
}
