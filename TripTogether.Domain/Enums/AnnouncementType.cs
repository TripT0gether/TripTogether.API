namespace TripTogether.Domain.Enums;

public enum AnnouncementType
{
    GroupCreated,
    GroupUpdated,
    GroupMemberJoined,
    GroupMemberLeft,
    
    TripCreated,
    TripUpdated,
    TripStatusChanged,
    TripDatesConfirmed,
    
    ActivityCreated,
    ActivityUpdated,
    ActivityScheduled,
    ActivityCompleted,
    
    PollCreated,
    PollClosed,
    
    PackingItemAssigned,
    PackingItemCompleted,
    
    FriendRequestReceived,
    FriendRequestAccepted,
    
    InviteCreated,
    InviteExpired,
    
    ExpenseAdded,
    SettlementCompleted,
    
    General
}
