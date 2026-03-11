using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.AnnouncementDTO;

public class CreateAnnouncementDto
{
    public AnnouncementType Type { get; set; }
    public required string Message { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? TripId { get; set; }
    public Guid? ActivityId { get; set; }
    public Guid? PollId { get; set; }
    public Guid? PackingItemId { get; set; }
    public Guid? FriendshipId { get; set; }
    public Guid? GroupInviteId { get; set; }
    public Guid? TargetUserId { get; set; }
    public Guid? FromUserId { get; set; }
}
