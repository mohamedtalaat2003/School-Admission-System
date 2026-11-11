using Microsoft.AspNetCore.Mvc;
using SchoolAdmissionSystem.Api.Filters;
using SchoolAdmissionSystem.Api.Models.Dtos;
using SchoolAdmissionSystem.Api.Services;

namespace SchoolAdmissionSystem.Api.Controllers;

[ApiController]
[Route("holds")]
[Produces("application/json")]
public class HoldsController : ControllerBase
{
    private readonly IHoldService _holdService;

    public HoldsController(IHoldService holdService)
    {
        _holdService = holdService;
    }

    /// <summary>
    /// GET /holds/{holdId}
    /// </summary>
    [HttpGet("{holdId}")]
    [ProducesResponseType(typeof(HoldStatusResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Hold([FromRoute] string holdId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(holdId))
        {
            throw new ArgumentException("Hold ID cannot be empty.", nameof(holdId));
        }

        var hold = await _holdService.GetHoldByIdAsync(holdId.Trim(), ct);
        if (hold == null)
        {
            throw new KeyNotFoundException($"Application with id '{holdId}' was not found.");
        }

        return Ok(hold);
    }

    /// <summary>
    /// POST /holds/{holdId}/confirm
    /// </summary>
    [HttpPost("{holdId}/confirm")]
    [RequireStudentId]
    [ProducesResponseType(typeof(ConfirmHoldResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Confirm([FromRoute] string holdId, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(holdId))
        {
            throw new ArgumentException("Hold ID cannot be empty.", nameof(holdId));
        }

        var studentId = (string)HttpContext.Items[RequireStudentIdAttribute.HeaderName]!;
        var result = await _holdService.ConfirmHoldAsync(holdId.Trim(), studentId.Trim(), ct);
        return Ok(result);
    }

    /// <summary>
    /// DELETE /holds/{holdId}
    /// </summary>
    [HttpDelete("{holdId}")]
    [RequireStudentId]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Hold([FromRoute] string holdId, [FromQuery] string? _unused, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(holdId))
        {
            throw new ArgumentException("Hold ID cannot be empty.", nameof(holdId));
        }

        var studentId = (string)HttpContext.Items[RequireStudentIdAttribute.HeaderName]!;
        await _holdService.CancelHoldAsync(holdId.Trim(), studentId.Trim(), ct);
        return NoContent();
    }
}
