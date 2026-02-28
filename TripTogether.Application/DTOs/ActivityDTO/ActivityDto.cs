using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.ActivityDTO;

public class ActivityDto
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public ActivityStatus Status { get; set; }
    public string Title { get; set; } = null!;
    public ActivityCategory? Category { get; set; }
    public DateOnly? Date { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? ScheduleDayIndex { get; set; }
    public string? LocationName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string? LinkUrl { get; set; }
    public string? ImageUrl { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
}
