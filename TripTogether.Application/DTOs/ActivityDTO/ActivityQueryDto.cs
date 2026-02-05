using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.ActivityDTO;

public class ActivityQueryDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string? SearchTerm { get; set; }
    public ActivityStatus? Status { get; set; }
    public ActivityCategory? Category { get; set; }
    public DateOnly? FromDate { get; set; }
    public DateOnly? ToDate { get; set; }

}
