namespace TripTogether.Application.DTOs.GroupDTO;

public class GroupDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? CoverPhotoUrl { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public int MemberCount { get; set; }
    public string? InviteToken { get; set; }
}
