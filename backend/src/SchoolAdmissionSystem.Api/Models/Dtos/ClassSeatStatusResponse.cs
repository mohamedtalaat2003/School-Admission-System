namespace SchoolAdmissionSystem.Api.Models.Dtos;

public class ClassSeatStatusResponse
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int TotalSeats { get; set; }
    public int HeldSeats { get; set; }
    public int ConfirmedSeats { get; set; }
    public int AvailableSeats { get; set; }
}
