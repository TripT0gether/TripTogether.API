namespace TripTogether.Application.DTOs.FriendshipDTO;

public class UserSearchResultDto
{
    public Guid Id { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public bool Gender { get; set; }
    public bool IsEmailVerified { get; set; }
}
