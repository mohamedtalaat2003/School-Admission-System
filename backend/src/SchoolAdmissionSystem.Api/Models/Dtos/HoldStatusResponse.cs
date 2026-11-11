namespace SchoolAdmissionSystem.Api.Models.Dtos;

public class HoldStatusResponse
{
    public string HoldId { get; set; } = string.Empty;
    public string ClassId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string ExpiresAt { get; set; } = string.Empty;
}
