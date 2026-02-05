namespace TripTogether.Application.DTOs.GroupDTO;

public sealed class GroupInvitationDto
{
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = null!;
    public string? CoverPhotoUrl { get; set; }
    public Guid InvitedBy { get; set; }
    public string InviterUsername { get; set; } = null!;
    public string? InviterAvatarUrl { get; set; }
    public DateTime InvitedAt { get; set; }
    public int MemberCount { get; set; }
}