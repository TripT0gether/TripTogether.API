namespace TripTogether.Application.DTOs.VoteDTO;

public class VoteDto
{
    public Guid Id { get; set; }
    public Guid PollOptionId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
}
