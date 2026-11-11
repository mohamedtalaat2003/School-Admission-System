namespace SchoolAdmissionSystem.Api.Models.Dtos;

public class ErrorResponse
{
    public int Status { get; set; }
    public string Error { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public string Timestamp { get; set; } = DateTimeOffset.UtcNow.ToString("o");
}
