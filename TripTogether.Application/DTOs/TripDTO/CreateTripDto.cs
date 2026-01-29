namespace TripTogether.Application.DTOs.TripDTO;

public class CreateTripDto
{
    public Guid GroupId { get; set; }
    public string Title { get; set; } = null!;
    public DateOnly? PlanningRangeStart { get; set; }
    public DateOnly? PlanningRangeEnd { get; set; }
}
