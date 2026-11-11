using System.ComponentModel.DataAnnotations;

namespace SchoolAdmissionSystem.Api.Models.Entities;

public class Class
{
    [Key]
    public string Id { get; set; } = string.Empty;

    [Required]
    public string Name { get; set; } = string.Empty;

    public int TotalSeats { get; set; }

    [ConcurrencyCheck]
    public int Version { get; set; } = 1;

    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    public ICollection<Hold> Holds { get; set; } = new List<Hold>();
}
