namespace TripTogether.Application.DTOs.TripDTO;

public class UpdateTripDto
{
    public string? Title { get; set; }
    public DateOnly? PlanningRangeStart { get; set; }
    public DateOnly? PlanningRangeEnd { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public TripSettings? Settings { get; set; }
    public decimal? Budget { get; set; }
}
