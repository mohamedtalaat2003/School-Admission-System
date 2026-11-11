using System.ComponentModel.DataAnnotations;
using SchoolAdmissionSystem.Api.Models.Enums;

namespace SchoolAdmissionSystem.Api.Models.Entities;

public class Hold
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string ClassId { get; set; } = string.Empty;

    [Required]
    public string StudentId { get; set; } = string.Empty;

    [Required]
    public HoldStatus Status { get; set; } = HoldStatus.HELD;

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ConfirmedAt { get; set; }

    public Class Class { get; set; } = null!;
}
