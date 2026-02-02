using System.ComponentModel.DataAnnotations;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.ActivityDTO;

public class UpdateActivityDto
{
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string? Title { get; set; }

    public ActivityStatus? Status { get; set; }
    public ActivityCategory? Category { get; set; }
    public DateOnly? Date { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }
    public int? ScheduleDayIndex { get; set; }
    public TimeSlot? ScheduleSlot { get; set; }

    [MaxLength(500, ErrorMessage = "Location name cannot exceed 500 characters")]
    public string? LocationName { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [Url(ErrorMessage = "Invalid URL format")]
    public string? LinkUrl { get; set; }

    [MaxLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters")]
    public string? Notes { get; set; }
}
