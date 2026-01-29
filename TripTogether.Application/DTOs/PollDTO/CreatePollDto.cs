using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.PollDTO;

public class CreatePollDto
{
    public Guid TripId { get; set; }
    public PollType Type { get; set; }
    public string Title { get; set; } = null!;
    public List<CreatePollOptionDto> Options { get; set; } = new();
}
