using System.Text.RegularExpressions;
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
    private static readonly Regex StudentIdRegex = new(@"^\d{17}$", RegexOptions.Compiled);
    private readonly IClassService _classService;
    private readonly IHoldService _holdService;
    private readonly ILogger<ClassesController> _logger;

    public ClassesController(IClassService classService, IHoldService holdService, ILogger<ClassesController> logger)
    {
        _classService = classService;
        _holdService = holdService;
        _logger = logger;
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
        if (!ModelState.IsValid)
        {
            return BadRequest(new ErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Error = "Bad Request",
                Message = string.Join("; ", ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage))
            });
        }

        try
        {
            var result = await _classService.CreateClassAsync(request, ct);
            return StatusCode(StatusCodes.Status201Created, result);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new ErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Error = "Bad Request",
                Message = ex.Message
            });
        }
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
            return BadRequest(new ErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Error = "Bad Request",
                Message = "Page must be greater than zero."
            });
        }

        if (pageSize.HasValue && pageSize.Value <= 0)
        {
            return BadRequest(new ErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Error = "Bad Request",
                Message = "Page size must be greater than zero."
            });
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
            return BadRequest(new ErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Error = "Bad Request",
                Message = "Class ID cannot be empty."
            });
        }

        var classItem = await _classService.GetClassByIdAsync(classId.Trim(), ct);
        if (classItem == null)
        {
            return NotFound(new ErrorResponse
            {
                Status = StatusCodes.Status404NotFound,
                Error = "Not Found",
                Message = $"Class with id '{classId}' does not exist."
            });
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
            return BadRequest(new ErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Error = "Bad Request",
                Message = "Class ID cannot be empty."
            });
        }

        var studentIdObj = HttpContext.Items[RequireStudentIdAttribute.HeaderName];
        var studentId = studentIdObj as string;

        if (string.IsNullOrWhiteSpace(studentId) || !StudentIdRegex.IsMatch(studentId.Trim()))
        {
            return BadRequest(new ErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Error = "Bad Request",
                Message = "Student ID must be a 17-digit birth registration number."
            });
        }

        try
        {
            var hold = await _holdService.CreateHoldAsync(classId.Trim(), studentId.Trim(), ct);
            return StatusCode(StatusCodes.Status201Created, hold);
        }
        catch (KeyNotFoundException ex)
        {
            return NotFound(new ErrorResponse
            {
                Status = StatusCodes.Status404NotFound,
                Error = "Not Found",
                Message = ex.Message
            });
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(new ErrorResponse
            {
                Status = StatusCodes.Status409Conflict,
                Error = "Conflict",
                Message = ex.Message
            });
        }
    }
}
