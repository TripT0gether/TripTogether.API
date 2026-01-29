using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.PollDTO;

public class PollDetailDto
{
    public Guid Id { get; set; }
    public Guid TripId { get; set; }
    public string TripTitle { get; set; } = null!;
    public PollType Type { get; set; }
    public string Title { get; set; } = null!;
    public PollStatus Status { get; set; }
    public Guid CreatedBy { get; set; }
    public string CreatorName { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public List<PollOptionDto> Options { get; set; } = new();
    public int TotalVotes { get; set; }
}
