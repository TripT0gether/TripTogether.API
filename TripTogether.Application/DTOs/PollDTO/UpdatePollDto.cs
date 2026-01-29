using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.PollDTO;

public class UpdatePollDto
{
    public string? Title { get; set; }
    public PollStatus? Status { get; set; }
}
