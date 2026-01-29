namespace TripTogether.Application.DTOs.GroupDTO;

public sealed class GroupMemberDto
{
    public Guid UserId { get; set; }
    public string Username { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? AvatarUrl { get; set; }
    public string Role { get; set; } = null!;
    public string Status { get; set; } = null!;
}