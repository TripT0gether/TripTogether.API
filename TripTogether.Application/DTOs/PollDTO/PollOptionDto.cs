using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.PollDTO;

public class PollOptionDto
{
    public Guid Id { get; set; }
    public Guid PollId { get; set; }
    public string? TextValue { get; set; }
    public string? MediaUrl { get; set; }
    public string? Metadata { get; set; }
    public DateTime? DateStart { get; set; }
    public DateTime? DateEnd { get; set; }
    public TimeSlot? TimeOfDay { get; set; }
    public int VoteCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
