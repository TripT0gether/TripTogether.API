using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TripTogether.Application.DTOs.AnnouncementDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Services;

public sealed class AnnouncementService : IAnnouncementService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly IHubContext<AnnouncementHub> _hubContext;
    private readonly ILogger<AnnouncementService> _logger;

    public AnnouncementService(
        IUnitOfWork unitOfWork,
        IClaimsService claimsService,
        IHubContext<AnnouncementHub> hubContext,
        ILogger<AnnouncementService> logger)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _hubContext = hubContext;
        _logger = logger;
    }

    public async Task<AnnouncementDto> CreateAnnouncementAsync(CreateAnnouncementDto dto)
    {
        var announcement = new Announcement
        {
            Type = dto.Type,
            Message = dto.Message,
            GroupId = dto.GroupId,
            TripId = dto.TripId,
            ActivityId = dto.ActivityId,
            PollId = dto.PollId,
            PackingItemId = dto.PackingItemId,
            FriendshipId = dto.FriendshipId,
            GroupInviteId = dto.GroupInviteId,
            TargetUserId = dto.TargetUserId,
            FromUserId = dto.FromUserId,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Announcements.AddAsync(announcement);
        await _unitOfWork.SaveChangesAsync();

        var announcementDto = await MapToDtoAsync(announcement);

        await SendAnnouncementViaSignalRAsync(announcementDto);

        _logger.LogInformation("Announcement {AnnouncementId} created: {Type}", announcement.Id, announcement.Type);

        return announcementDto;
    }

    public async Task<AnnouncementDto> GetAnnouncementByIdAsync(Guid announcementId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        var announcement = await _unitOfWork.Announcements.GetQueryable()
            .Include(a => a.Group)
            .Include(a => a.Trip)
            .Include(a => a.FromUser)
            .FirstOrDefaultAsync(a => a.Id == announcementId && !a.IsDeleted);

        if (announcement == null)
        {
            throw ErrorHelper.NotFound("The announcement does not exist.");
        }

        if (announcement.TargetUserId.HasValue && announcement.TargetUserId != currentUserId)
        {
            throw ErrorHelper.Forbidden("You cannot access this announcement.");
        }

        return await MapToDtoAsync(announcement);
    }

    public async Task<Pagination<AnnouncementDto>> GetMyAnnouncementsAsync(AnnouncementQueryDto query)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        // Get all group IDs where user is an active member
        var userGroupIds = await _unitOfWork.GroupMembers.GetQueryable()
            .Where(gm => gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Active)
            .Select(gm => gm.GroupId)
            .ToListAsync();

        // Get all trip IDs where user is a member (through their groups)
        var userTripIds = await _unitOfWork.Trips.GetQueryable()
            .Where(t => userGroupIds.Contains(t.GroupId) && !t.IsDeleted)
            .Select(t => t.Id)
            .ToListAsync();

        var announcementsQuery = _unitOfWork.Announcements.GetQueryable()
            .Include(a => a.Group)
            .Include(a => a.Trip)
            .Include(a => a.FromUser)
            .Where(a => !a.IsDeleted &&
                       (a.TargetUserId == currentUserId || // Targeted to user
                        (a.TargetUserId == null && a.GroupId.HasValue && userGroupIds.Contains(a.GroupId.Value)) || // Group announcement
                        (a.TargetUserId == null && a.TripId.HasValue && userTripIds.Contains(a.TripId.Value)))); // Trip announcement

        if (query.Type.HasValue)
        {
            announcementsQuery = announcementsQuery.Where(a => a.Type == query.Type.Value);
        }

        if (query.IsRead.HasValue)
        {
            announcementsQuery = announcementsQuery.Where(a => a.IsRead == query.IsRead.Value);
        }

        if (query.GroupId.HasValue)
        {
            announcementsQuery = announcementsQuery.Where(a => a.GroupId == query.GroupId.Value);
        }

        if (query.TripId.HasValue)
        {
            announcementsQuery = announcementsQuery.Where(a => a.TripId == query.TripId.Value);
        }

        var totalCount = await announcementsQuery.CountAsync();

        var announcements = await announcementsQuery
            .OrderByDescending(a => a.CreatedAt)
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var announcementDtos = new List<AnnouncementDto>();
        foreach (var announcement in announcements)
        {
            announcementDtos.Add(await MapToDtoAsync(announcement));
        }

        return new Pagination<AnnouncementDto>(announcementDtos, totalCount, query.PageNumber, query.PageSize);
    }

    public async Task<bool> MarkAsReadAsync(Guid announcementId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        var announcement = await _unitOfWork.Announcements.GetQueryable()
            .FirstOrDefaultAsync(a => a.Id == announcementId && !a.IsDeleted);

        if (announcement == null)
        {
            throw ErrorHelper.NotFound("The announcement does not exist.");
        }

        if (announcement.TargetUserId.HasValue && announcement.TargetUserId != currentUserId)
        {
            throw ErrorHelper.Forbidden("You cannot mark this announcement as read.");
        }

        announcement.IsRead = true;
        announcement.ReadAt = DateTime.UtcNow;

        await _unitOfWork.Announcements.Update(announcement);
        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<bool> MarkAllAsReadAsync()
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        // Get all group IDs where user is an active member
        var userGroupIds = await _unitOfWork.GroupMembers.GetQueryable()
            .Where(gm => gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Active)
            .Select(gm => gm.GroupId)
            .ToListAsync();

        // Get all trip IDs where user is a member (through their groups)
        var userTripIds = await _unitOfWork.Trips.GetQueryable()
            .Where(t => userGroupIds.Contains(t.GroupId) && !t.IsDeleted)
            .Select(t => t.Id)
            .ToListAsync();

        var unreadAnnouncements = await _unitOfWork.Announcements.GetQueryable()
            .Where(a => !a.IsDeleted &&
                       !a.IsRead &&
                       (a.TargetUserId == currentUserId || // Targeted to user
                        (a.TargetUserId == null && a.GroupId.HasValue && userGroupIds.Contains(a.GroupId.Value)) || // Group announcement
                        (a.TargetUserId == null && a.TripId.HasValue && userTripIds.Contains(a.TripId.Value)) || // Trip announcement
                        (a.TargetUserId == null && !a.GroupId.HasValue && !a.TripId.HasValue))) // Global announcement
            .ToListAsync();

        foreach (var announcement in unreadAnnouncements)
        {
            announcement.IsRead = true;
            announcement.ReadAt = DateTime.UtcNow;
            await _unitOfWork.Announcements.Update(announcement);
        }

        await _unitOfWork.SaveChangesAsync();

        return true;
    }

    public async Task<int> GetUnreadCountAsync()
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        // Get all group IDs where user is an active member
        var userGroupIds = await _unitOfWork.GroupMembers.GetQueryable()
            .Where(gm => gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Active)
            .Select(gm => gm.GroupId)
            .ToListAsync();

        // Get all trip IDs where user is a member (through their groups)
        var userTripIds = await _unitOfWork.Trips.GetQueryable()
            .Where(t => userGroupIds.Contains(t.GroupId) && !t.IsDeleted)
            .Select(t => t.Id)
            .ToListAsync();

        return await _unitOfWork.Announcements.GetQueryable()
            .CountAsync(a => !a.IsDeleted &&
                            !a.IsRead &&
                            (a.TargetUserId == currentUserId || // Targeted to user
                             (a.TargetUserId == null && a.GroupId.HasValue && userGroupIds.Contains(a.GroupId.Value)) || // Group announcement
                             (a.TargetUserId == null && a.TripId.HasValue && userTripIds.Contains(a.TripId.Value)) || // Trip announcement
                             (a.TargetUserId == null && !a.GroupId.HasValue && !a.TripId.HasValue))); // Global announcement
    }

    public async Task NotifyGroupCreatedAsync(Guid groupId, string groupName, Guid createdByUserId)
    {
        var creator = await _unitOfWork.Users.GetByIdAsync(createdByUserId);
        var creatorName = creator?.Username ?? "Unknown";

        var dto = new CreateAnnouncementDto
        {
            Type = AnnouncementType.GroupCreated,
            Message = $"Group '{groupName}' has been created by {creatorName}.",
            GroupId = groupId,
            FromUserId = createdByUserId
        };

        await CreateAnnouncementAsync(dto);
    }

    public async Task NotifyGroupMemberJoinedAsync(Guid groupId, string groupName, string memberName, Guid joinedUserId)
    {
        var dto = new CreateAnnouncementDto
        {
            Type = AnnouncementType.GroupMemberJoined,
            Message = $"{memberName} joined the group '{groupName}'.",
            GroupId = groupId,
            FromUserId = joinedUserId
        };

        await CreateAnnouncementAsync(dto);
    }

    public async Task NotifyGroupMemberLeftAsync(Guid groupId, string groupName, string memberName, Guid leftUserId)
    {
        var dto = new CreateAnnouncementDto
        {
            Type = AnnouncementType.GroupMemberLeft,
            Message = $"{memberName} left the group '{groupName}'.",
            GroupId = groupId,
            FromUserId = leftUserId
        };

        await CreateAnnouncementAsync(dto);
    }

    public async Task NotifyTripCreatedAsync(Guid tripId, Guid groupId, string tripTitle, Guid createdByUserId)
    {
        var creator = await _unitOfWork.Users.GetByIdAsync(createdByUserId);
        var creatorName = creator?.Username ?? "Unknown";

        var dto = new CreateAnnouncementDto
        {
            Type = AnnouncementType.TripCreated,
            Message = $"New trip '{tripTitle}' has been created by {creatorName}.",
            TripId = tripId,
            GroupId = groupId,
            FromUserId = createdByUserId
        };

        await CreateAnnouncementAsync(dto);
    }

    public async Task NotifyTripStatusChangedAsync(Guid tripId, Guid groupId, string tripTitle, TripStatus newStatus, Guid changedByUserId)
    {
        var changer = await _unitOfWork.Users.GetByIdAsync(changedByUserId);
        var changerName = changer?.Username ?? "Unknown";

        var dto = new CreateAnnouncementDto
        {
            Type = AnnouncementType.TripStatusChanged,
            Message = $"Trip '{tripTitle}' status changed to {newStatus} by {changerName}.",
            TripId = tripId,
            GroupId = groupId,
            FromUserId = changedByUserId
        };

        await CreateAnnouncementAsync(dto);
    }

    public async Task NotifyActivityCreatedAsync(Guid activityId, Guid tripId, Guid groupId, string activityTitle, Guid createdByUserId)
    {
        var creator = await _unitOfWork.Users.GetByIdAsync(createdByUserId);
        var creatorName = creator?.Username ?? "Unknown";

        var dto = new CreateAnnouncementDto
        {
            Type = AnnouncementType.ActivityCreated,
            Message = $"New activity '{activityTitle}' has been created by {creatorName}.",
            ActivityId = activityId,
            TripId = tripId,
            GroupId = groupId,
            FromUserId = createdByUserId
        };

        await CreateAnnouncementAsync(dto);
    }

    public async Task NotifyActivityScheduledAsync(Guid activityId, Guid tripId, Guid groupId, string activityTitle, DateOnly date, Guid scheduledByUserId)
    {
        var scheduler = await _unitOfWork.Users.GetByIdAsync(scheduledByUserId);
        var schedulerName = scheduler?.Username ?? "Unknown";

        var dto = new CreateAnnouncementDto
        {
            Type = AnnouncementType.ActivityScheduled,
            Message = $"Activity '{activityTitle}' has been scheduled for {date:MMMM dd, yyyy} by {schedulerName}.",
            ActivityId = activityId,
            TripId = tripId,
            GroupId = groupId,
            FromUserId = scheduledByUserId
        };

        await CreateAnnouncementAsync(dto);
    }

    public async Task NotifyPollCreatedAsync(Guid pollId, Guid tripId, Guid groupId, string pollTitle, PollType pollType, Guid createdByUserId)
    {
        var creator = await _unitOfWork.Users.GetByIdAsync(createdByUserId);
        var creatorName = creator?.Username ?? "Unknown";

        var dto = new CreateAnnouncementDto
        {
            Type = AnnouncementType.PollCreated,
            Message = $"New {pollType} poll '{pollTitle}' has been created by {creatorName}.",
            PollId = pollId,
            TripId = tripId,
            GroupId = groupId,
            FromUserId = createdByUserId
        };

        await CreateAnnouncementAsync(dto);
    }

    public async Task NotifyPollClosedAsync(Guid pollId, Guid tripId, Guid groupId, string pollTitle, Guid closedByUserId)
    {
        var closer = await _unitOfWork.Users.GetByIdAsync(closedByUserId);
        var closerName = closer?.Username ?? "Unknown";

        var dto = new CreateAnnouncementDto
        {
            Type = AnnouncementType.PollClosed,
            Message = $"Poll '{pollTitle}' has been closed by {closerName}.",
            PollId = pollId,
            TripId = tripId,
            GroupId = groupId,
            FromUserId = closedByUserId
        };

        await CreateAnnouncementAsync(dto);
    }

    public async Task NotifyPackingItemAssignedAsync(Guid packingItemId, Guid tripId, Guid groupId, string itemName, Guid assignedUserId, Guid assignedByUserId)
    {
        var assigner = await _unitOfWork.Users.GetByIdAsync(assignedByUserId);
        var assignerName = assigner?.Username ?? "Unknown";

        var dto = new CreateAnnouncementDto
        {
            Type = AnnouncementType.PackingItemAssigned,
            Message = $"You have been assigned to bring '{itemName}' by {assignerName}.",
            PackingItemId = packingItemId,
            TripId = tripId,
            GroupId = groupId,
            TargetUserId = assignedUserId,
            FromUserId = assignedByUserId
        };

        await CreateAnnouncementAsync(dto);
    }

    public async Task NotifyFriendRequestReceivedAsync(Guid friendshipId, Guid addresseeId, string requesterName, Guid requesterId)
    {
        var dto = new CreateAnnouncementDto
        {
            Type = AnnouncementType.FriendRequestReceived,
            Message = $"{requesterName} sent you a friend request.",
            FriendshipId = friendshipId,
            TargetUserId = addresseeId,
            FromUserId = requesterId
        };

        await CreateAnnouncementAsync(dto);
    }

    public async Task NotifyFriendRequestAcceptedAsync(Guid friendshipId, Guid requesterId, string addresseeName, Guid addresseeId)
    {
        var dto = new CreateAnnouncementDto
        {
            Type = AnnouncementType.FriendRequestAccepted,
            Message = $"{addresseeName} accepted your friend request.",
            FriendshipId = friendshipId,
            TargetUserId = requesterId,
            FromUserId = addresseeId
        };

        await CreateAnnouncementAsync(dto);
    }

    public async Task NotifyInviteCreatedAsync(Guid inviteId, Guid groupId, string groupName, Guid createdByUserId)
    {
        var creator = await _unitOfWork.Users.GetByIdAsync(createdByUserId);
        var creatorName = creator?.Username ?? "Unknown";

        var dto = new CreateAnnouncementDto
        {
            Type = AnnouncementType.InviteCreated,
            Message = $"A new invite link has been created for '{groupName}' by {creatorName}.",
            GroupInviteId = inviteId,
            GroupId = groupId,
            FromUserId = createdByUserId
        };

        await CreateAnnouncementAsync(dto);
    }

    private async Task<AnnouncementDto> MapToDtoAsync(Announcement announcement)
    {
        return new AnnouncementDto
        {
            Id = announcement.Id,
            Type = announcement.Type,
            Message = announcement.Message,
            GroupId = announcement.GroupId,
            GroupName = announcement.Group?.Name,
            TripId = announcement.TripId,
            TripTitle = announcement.Trip?.Title,
            ActivityId = announcement.ActivityId,
            PollId = announcement.PollId,
            PackingItemId = announcement.PackingItemId,
            FriendshipId = announcement.FriendshipId,
            GroupInviteId = announcement.GroupInviteId,
            TargetUserId = announcement.TargetUserId,
            FromUserId = announcement.FromUserId,
            FromUserName = announcement.FromUser?.Username,
            IsRead = announcement.IsRead,
            ReadAt = announcement.ReadAt,
            CreatedAt = announcement.CreatedAt
        };
    }

    private async Task SendAnnouncementViaSignalRAsync(AnnouncementDto announcement)
    {
        try
        {
            if (announcement.TargetUserId.HasValue)
            {
                await _hubContext.Clients
                    .Group($"user_{announcement.TargetUserId}")
                    .SendAsync("ReceiveAnnouncement", announcement);
            }
            else if (announcement.TripId.HasValue)
            {
                await _hubContext.Clients
                    .Group($"trip_{announcement.TripId}")
                    .SendAsync("ReceiveAnnouncement", announcement);
            }
            else if (announcement.GroupId.HasValue)
            {
                await _hubContext.Clients
                    .Group($"group_{announcement.GroupId}")
                    .SendAsync("ReceiveAnnouncement", announcement);
            }
            else
            {
                await _hubContext.Clients.All.SendAsync("ReceiveAnnouncement", announcement);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send announcement via SignalR");
        }
    }
}
