using Microsoft.AspNetCore.Mvc;
using SchoolAdmissionSystem.Api.Filters;
using SchoolAdmissionSystem.Api.Models.Dtos;
using SchoolAdmissionSystem.Api.Services;

namespace SchoolAdmissionSystem.Api.Controllers;

[ApiController]
[Route("classes")]
[Produces("application/json")]
public class ClassesController : ControllerBase
{
    private readonly IClassService _classService;
    private readonly IHoldService _holdService;

    public ClassesController(IClassService classService, IHoldService holdService)
    {
        _classService = classService;
        _holdService = holdService;
    }

    /// <summary>
    /// POST /classes
    /// </summary>
    [HttpPost]
    [Consumes("application/json")]
    [ProducesResponseType(typeof(ClassResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Class([FromBody] CreateClassRequest request, CancellationToken ct)
    {
        var result = await _classService.CreateClassAsync(request, ct);
        return StatusCode(StatusCodes.Status201Created, result);
    }

    /// <summary>
    /// GET /classes
    /// </summary>
    [HttpGet]
    [ProducesResponseType(typeof(IReadOnlyList<ClassSeatStatusResponse>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Classes([FromQuery] int? page, [FromQuery] int? pageSize, CancellationToken ct)
    {
        if (page.HasValue && page.Value <= 0)
        {
            throw new ArgumentException("Page must be greater than zero.", nameof(page));
        }

        if (pageSize.HasValue && pageSize.Value <= 0)
        {
            throw new ArgumentException("Page size must be greater than zero.", nameof(pageSize));
        }

        var classes = await _classService.GetClassesAsync(page, pageSize, ct);
        return Ok(classes);
    }

    /// <summary>
    /// GET /classes/{classId}
    /// </summary>
    [HttpGet("{classId}")]
    [ProducesResponseType(typeof(ClassSeatStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Class([FromRoute] string classId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(classId))
        {
            throw new ArgumentException("Class ID cannot be empty.", nameof(classId));
        }

        var classItem = await _classService.GetClassByIdAsync(classId.Trim(), ct);
        if (classItem == null)
        {
            throw new KeyNotFoundException($"Class with id '{classId}' does not exist.");
        }

        return Ok(classItem);
    }

    /// <summary>
    /// POST /classes/{classId}/holds
    /// </summary>
    [HttpPost("{classId}/holds")]
    [RequireStudentId]
    [ProducesResponseType(typeof(HoldResponse), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Holds([FromRoute] string classId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(classId))
        {
            throw new ArgumentException("Class ID cannot be empty.", nameof(classId));
        }

        var studentId = (string)HttpContext.Items[RequireStudentIdAttribute.HeaderName]!;
        var hold = await _holdService.CreateHoldAsync(classId.Trim(), studentId.Trim(), ct);
        return StatusCode(StatusCodes.Status201Created, hold);
    }
}
