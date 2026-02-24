using System.ComponentModel;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.PollDTO;

public class CreatePollOptionDto
{
    public int? Index { get; set; }
    public string? TextValue { get; set; }
    public string? MediaUrl { get; set; }

    [DefaultValue("1000000")]
    public string? Metadata { get; set; }

    // For Date Voting
    public DateTime? DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    public TimeSlot? TimeOfDay { get; set; }
}
