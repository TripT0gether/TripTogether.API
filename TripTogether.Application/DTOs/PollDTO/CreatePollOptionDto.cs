using System.ComponentModel;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.PollDTO;

public class CreatePollOptionDto
{
    public string? TextValue { get; set; }
    public string? MediaUrl { get; set; }

    [DefaultValue("1000000")]
    public string? Metadata { get; set; }

    // For Date Voting
    public DateOnly? StartDate { get; set; }
    public DateOnly? EndDate { get; set; }

    // For Time Voting
    public TimeOnly? StartTime { get; set; }
    public TimeOnly? EndTime { get; set; }

    // Time of Day Category
    public TimeSlot? TimeOfDay { get; set; }
}
