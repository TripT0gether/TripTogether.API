using System.ComponentModel.DataAnnotations;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.ActivityDTO;

public class CreateActivityDto
{
    [Required(ErrorMessage = "Trip ID is required")]
    public Guid TripId { get; set; }

    [Required(ErrorMessage = "Title is required")]
    [MaxLength(200, ErrorMessage = "Title cannot exceed 200 characters")]
    public string Title { get; set; } = null!;

    public ActivityStatus Status { get; set; } = ActivityStatus.Idea;
    public ActivityCategory? Category { get; set; }
    public DateOnly Date { get; set; }
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    [MaxLength(500, ErrorMessage = "Location name cannot exceed 500 characters")]
    public string? LocationName { get; set; }

    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    [Url(ErrorMessage = "Invalid URL format")]
    public string? LinkUrl { get; set; }

    [MaxLength(2000, ErrorMessage = "Notes cannot exceed 2000 characters")]
    public string? Notes { get; set; }
}
