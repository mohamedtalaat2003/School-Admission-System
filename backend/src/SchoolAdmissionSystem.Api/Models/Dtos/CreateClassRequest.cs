using System.ComponentModel.DataAnnotations;

namespace SchoolAdmissionSystem.Api.Models.Dtos;

public class CreateClassRequest
{
    [Required(AllowEmptyStrings = false, ErrorMessage = "Class name is required.")]
    public string Name { get; set; } = string.Empty;

    [Range(1, int.MaxValue, ErrorMessage = "Total seats must be greater than zero.")]
    public int TotalSeats { get; set; }
}
