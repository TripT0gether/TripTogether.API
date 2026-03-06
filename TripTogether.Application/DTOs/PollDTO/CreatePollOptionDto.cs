using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.PollDTO;

public class CreatePollOptionDto
{
    public string? TextValue { get; set; }
    public string? MediaUrl { get; set; }
    public decimal? Budget { get; set; }

    // For Date Voting
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    // For Time Voting
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    // Time of Day Category
    public TimeSlot? TimeOfDay { get; set; }
}
