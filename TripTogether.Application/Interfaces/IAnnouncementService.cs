using TripTogether.Application.DTOs.AnnouncementDTO;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Interfaces;

public interface IAnnouncementService
{
    Task<AnnouncementDto> CreateAnnouncementAsync(CreateAnnouncementDto dto);
    Task<AnnouncementDto> GetAnnouncementByIdAsync(Guid announcementId);
    Task<Pagination<AnnouncementDto>> GetMyAnnouncementsAsync(AnnouncementQueryDto query);
    Task<bool> MarkAsReadAsync(Guid announcementId);
    Task<bool> MarkAllAsReadAsync();
    Task<int> GetUnreadCountAsync();

    // Helper methods for automatic announcement creation
    Task NotifyGroupCreatedAsync(Guid groupId, string groupName, Guid createdByUserId);
    Task NotifyGroupMemberJoinedAsync(Guid groupId, string groupName, string memberName, Guid joinedUserId);
    Task NotifyGroupMemberLeftAsync(Guid groupId, string groupName, string memberName, Guid leftUserId);
    Task NotifyTripCreatedAsync(Guid tripId, Guid groupId, string tripTitle, Guid createdByUserId);
    Task NotifyTripStatusChangedAsync(Guid tripId, Guid groupId, string tripTitle, TripStatus newStatus, Guid changedByUserId);
    Task NotifyActivityCreatedAsync(Guid activityId, Guid tripId, Guid groupId, string activityTitle, Guid createdByUserId);
    Task NotifyActivityScheduledAsync(Guid activityId, Guid tripId, Guid groupId, string activityTitle, DateOnly date, Guid scheduledByUserId);
    Task NotifyPollCreatedAsync(Guid pollId, Guid tripId, Guid groupId, string pollTitle, PollType pollType, Guid createdByUserId);
    Task NotifyPollClosedAsync(Guid pollId, Guid tripId, Guid groupId, string pollTitle, Guid closedByUserId);
    Task NotifyPackingItemAssignedAsync(Guid packingItemId, Guid tripId, Guid groupId, string itemName, Guid assignedUserId, Guid assignedByUserId);
    Task NotifyFriendRequestReceivedAsync(Guid friendshipId, Guid addresseeId, string requesterName, Guid requesterId);
    Task NotifyFriendRequestAcceptedAsync(Guid friendshipId, Guid requesterId, string addresseeName, Guid addresseeId);
    Task NotifyInviteCreatedAsync(Guid inviteId, Guid groupId, string groupName, Guid createdByUserId);
}
