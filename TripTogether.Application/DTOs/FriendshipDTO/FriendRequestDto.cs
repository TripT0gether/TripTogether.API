namespace TripTogether.Application.DTOs.FriendshipDTO;

public sealed class FriendRequestDto
{
    public Guid FriendshipId { get; set; }
    public Guid UserId { get; set; }
    public string Username { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public DateTime RequestDate { get; set; }
}
