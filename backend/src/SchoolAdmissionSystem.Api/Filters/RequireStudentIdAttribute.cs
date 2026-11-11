using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using SchoolAdmissionSystem.Api.Models.Dtos;

namespace SchoolAdmissionSystem.Api.Filters;

[AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
public class RequireStudentIdAttribute : ActionFilterAttribute
{
    public const string HeaderName = "X-Student-Id";
    private static readonly Regex StudentIdRegex = new(@"^\d{17}$", RegexOptions.Compiled);

    public override void OnActionExecuting(ActionExecutingContext context)
    {
        if (!context.HttpContext.Request.Headers.TryGetValue(HeaderName, out var values) || string.IsNullOrWhiteSpace(values))
        {
            context.Result = new BadRequestObjectResult(new ErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Error = "Bad Request",
                Message = $"Header '{HeaderName}' is required."
            });
            return;
        }

        var studentId = values.ToString().Trim();
        if (!StudentIdRegex.IsMatch(studentId))
        {
            context.Result = new BadRequestObjectResult(new ErrorResponse
            {
                Status = StatusCodes.Status400BadRequest,
                Error = "Bad Request",
                Message = $"'{HeaderName}' must be a 17-digit birth registration number."
            });
            return;
        }

        context.HttpContext.Items[HeaderName] = studentId;
        base.OnActionExecuting(context);
    }
}
