namespace TripTogether.Application.DTOs.ActivityDTO;

public class ActivitiesByDateDto
{
    public DateOnly? Date { get; set; }
    public List<ActivityDto> Activities { get; set; } = new();
    public int TotalActivities { get; set; }
}
