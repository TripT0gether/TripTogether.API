using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TripTogether.Application.DTOs.GroupDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Application.Utils;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Services;

public sealed class GroupService : IGroupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly IBlobService _blobService;
    private readonly ILogger _loggerService;

    public GroupService(
        IUnitOfWork unitOfWork,
        IClaimsService claimsService,
        IBlobService blobService,
        ILogger<GroupService> loggerService)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _blobService = blobService;
        _loggerService = loggerService;
    }

    public async Task<GroupDto> CreateGroupAsync(CreateGroupDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} creating group: {GroupName}", currentUserId, dto.Name);

        var group = new Group
        {
            Name = dto.Name,
            CreatedBy = currentUserId
        };

        await _unitOfWork.Groups.AddAsync(group);

        var creatorMember = new GroupMember
        {
            GroupId = group.Id,
            UserId = currentUserId,
            Role = GroupMemberRole.Leader,
            Status = GroupMemberStatus.Active
        };

        await _unitOfWork.GroupMembers.AddAsync(creatorMember);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Group {GroupId} created successfully by user {CurrentUserId}", group.Id, currentUserId);

        return new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            CoverPhotoUrl = group.CoverPhotoUrl,
            CreatedBy = group.CreatedBy,
            CreatedAt = group.CreatedAt,
            MemberCount = 1
        };
    }

    public async Task<GroupDto> UpdateGroupAsync(Guid groupId, UpdateGroupDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} updating group {GroupId}", currentUserId, groupId);

        if (!await IsGroupLeaderAsync(currentUserId, groupId))
        {
            throw ErrorHelper.Forbidden("The new team leader has the authority to update team information.");
        }

        var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
        if (group == null)
        {
            throw ErrorHelper.NotFound("The group does not exist.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Name))
        {
            group.Name = dto.Name;
        }

        await _unitOfWork.Groups.Update(group);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Group {GroupId} updated successfully", groupId);

        var memberCount = await _unitOfWork.GroupMembers.GetQueryable()
            .CountAsync(gm => gm.GroupId == groupId && gm.Status == GroupMemberStatus.Active);

        return new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            CoverPhotoUrl = group.CoverPhotoUrl,
            CreatedBy = group.CreatedBy,
            CreatedAt = group.CreatedAt,
            MemberCount = memberCount
        };
    }

    public async Task<string> UploadCoverPhotoAsync(Guid groupId, IFormFile file)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} uploading cover photo for group {GroupId}", currentUserId, groupId);

        if (!await IsGroupLeaderAsync(currentUserId, groupId))
        {
            throw ErrorHelper.Forbidden("Only the team leader has the right to change the cover photo.");
        }

        var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
        if (group == null)
        {
            throw ErrorHelper.NotFound("The group does not exist.");
        }
        var fileName = $"{groupId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";

        using var stream = file.OpenReadStream();
        await _blobService.UploadFileAsync(fileName, stream, "group-covers");

        var coverPhotoUrl = await _blobService.GetFileUrlAsync($"group-covers/{fileName}");

        group.CoverPhotoUrl = coverPhotoUrl;

        await _unitOfWork.Groups.Update(group);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Cover photo uploaded successfully for group {GroupId}", groupId);

        return coverPhotoUrl;
    }

    public async Task<bool> DeleteGroupAsync(Guid groupId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} deleting group {GroupId}", currentUserId, groupId);

        if (!await IsGroupLeaderAsync(currentUserId, groupId))
        {
            throw ErrorHelper.Forbidden("Only the group leader has the right to delete a group.");
        }

        var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
        if (group == null)
        {
            throw ErrorHelper.NotFound("The group does not exist.");
        }

        await _unitOfWork.Groups.SoftRemove(group);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Group {GroupId} deleted successfully", groupId);

        return true;
    }

    public async Task<GroupDetailDto> GetGroupDetailAsync(Guid groupId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} getting details for group {GroupId}", currentUserId, groupId);

        var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
        if (group == null)
        {
            throw ErrorHelper.NotFound("The group does not exist.");
        }

        if (!await IsMemberAsync(currentUserId, groupId))
        {
            throw ErrorHelper.Forbidden("You are not a member of this group.");
        }

        var members = await _unitOfWork.GroupMembers.GetAllAsync(
            gm => gm.GroupId == groupId,
            gm => gm.User
        );

        return new GroupDetailDto
        {
            Id = group.Id,
            Name = group.Name,
            CoverPhotoUrl = group.CoverPhotoUrl,
            CreatedBy = group.CreatedBy,
            CreatedAt = group.CreatedAt,
            Members = members.Select(gm => new GroupMemberDto
            {
                UserId = gm.UserId,
                Username = gm.User.Username,
                Email = gm.User.Email,
                AvatarUrl = gm.User.AvatarUrl,
                Role = gm.Role.ToString(),
                Status = gm.Status.ToString()
            }).ToList()
        };
    }

    public async Task<Pagination<GroupDto>> GetMyGroupsAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string? sortBy = null,
        bool ascending = true)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("Getting groups for user {CurrentUserId}", currentUserId);

        IQueryable<GroupMember> groupMembersQuery = _unitOfWork.GroupMembers.GetQueryable()
            .Where(gm => gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Active);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            groupMembersQuery = groupMembersQuery
                .Include(gm => gm.Group)
                .Where(gm => gm.Group.Name.ToLower().Contains(lowerSearchTerm));
        }
        else
        {
            groupMembersQuery = groupMembersQuery.Include(gm => gm.Group);
        }

        var totalCount = await groupMembersQuery.CountAsync();

        var sortByLower = sortBy?.ToLower();
        var sortByMemberCount = sortByLower is "membercount" or "members";

        List<Guid> userGroupIds;
        Dictionary<Guid, int> memberCounts;

        if (!sortByMemberCount)
        {
            var orderedQuery = ascending
                ? groupMembersQuery.OrderBy(gm => gm.Group.CreatedAt).ThenBy(gm => gm.GroupId)
                : groupMembersQuery.OrderByDescending(gm => gm.Group.CreatedAt).ThenBy(gm => gm.GroupId);

            userGroupIds = await orderedQuery
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(gm => gm.GroupId)
                .ToListAsync();

            memberCounts = await _unitOfWork.GroupMembers.GetQueryable()
                .Where(gm => userGroupIds.Contains(gm.GroupId) && gm.Status == GroupMemberStatus.Active)
                .GroupBy(gm => gm.GroupId)
                .Select(g => new { GroupId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GroupId, x => x.Count);

            var groups = await _unitOfWork.Groups.GetQueryable()
                .Where(g => userGroupIds.Contains(g.Id))
                .ToListAsync();

            var groupDtos = groups.Select(g => new GroupDto
            {
                Id = g.Id,
                Name = g.Name,
                CoverPhotoUrl = g.CoverPhotoUrl,
                CreatedBy = g.CreatedBy,
                CreatedAt = g.CreatedAt,
                MemberCount = memberCounts.GetValueOrDefault(g.Id, 0)
            })
            .OrderBy(g => userGroupIds.IndexOf(g.Id))
            .ToList();

            return new Pagination<GroupDto>(groupDtos, totalCount, pageNumber, pageSize);
        }
        else
        {
            userGroupIds = await groupMembersQuery
                .Select(gm => gm.GroupId)
                .ToListAsync();

            memberCounts = await _unitOfWork.GroupMembers.GetQueryable()
                .Where(gm => userGroupIds.Contains(gm.GroupId) && gm.Status == GroupMemberStatus.Active)
                .GroupBy(gm => gm.GroupId)
                .Select(g => new { GroupId = g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.GroupId, x => x.Count);

            var allGroups = await _unitOfWork.Groups.GetQueryable()
                .Where(g => userGroupIds.Contains(g.Id))
                .ToListAsync();

            var groupDtos = allGroups.Select(g => new GroupDto
            {
                Id = g.Id,
                Name = g.Name,
                CoverPhotoUrl = g.CoverPhotoUrl,
                CreatedBy = g.CreatedBy,
                CreatedAt = g.CreatedAt,
                MemberCount = memberCounts.GetValueOrDefault(g.Id, 0)
            });

            var sortedGroups = ascending
                ? groupDtos.OrderBy(g => g.MemberCount).ThenBy(g => g.CreatedAt)
                : groupDtos.OrderByDescending(g => g.MemberCount).ThenBy(g => g.CreatedAt);

            var pagedGroups = sortedGroups
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            return new Pagination<GroupDto>(pagedGroups, totalCount, pageNumber, pageSize);
        }
    }

    public async Task<GroupDto> JoinGroupByToken(string token)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} joining group with token", currentUserId);

        var tripInvite = await _unitOfWork.TripInvites.FirstOrDefaultAsync(invite => invite.Token == token);
        if (tripInvite == null)
        {
            throw ErrorHelper.NotFound("The group does not exist or the token is invalid.");
        }

        var trip = await _unitOfWork.Trips.GetByIdAsync(tripInvite.TripId);
        if (trip == null)
        {
            throw ErrorHelper.NotFound("The trip associated with the invite does not exist.");
        }

        var group = await _unitOfWork.Groups.GetByIdAsync(trip.GroupId);
        if (group == null)
        {
            throw ErrorHelper.NotFound("The group does not exist or the token is invalid.");
        }

        var existingMember = await _unitOfWork.GroupMembers.FirstOrDefaultAsync(gm =>
            gm.GroupId == group.Id && gm.UserId == currentUserId);

        if (existingMember != null)
        {
            if (existingMember.Status == GroupMemberStatus.Active)
            {
                throw ErrorHelper.Conflict("You are already a member of the group.");
            }
            if (existingMember.Status == GroupMemberStatus.Pending)
            {
                throw ErrorHelper.Conflict("You have a pending invitation to the group.");
            }
        }

        var newMember = new GroupMember
        {
            GroupId = group.Id,
            UserId = currentUserId,
            Role = GroupMemberRole.Member,
            Status = GroupMemberStatus.Active
        };

        await _unitOfWork.GroupMembers.AddAsync(newMember);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("User {CurrentUserId} joined group {GroupId} successfully", currentUserId, group.Id);

        return new GroupDto
        {
            Id = group.Id,
            Name = group.Name,
            CoverPhotoUrl = group.CoverPhotoUrl,
            CreatedBy = group.CreatedBy,
            CreatedAt = group.CreatedAt,
            MemberCount = await _unitOfWork.GroupMembers.GetQueryable()
                .CountAsync(gm => gm.GroupId == group.Id && gm.Status == GroupMemberStatus.Active)
        };

    }

    public async Task<GroupMemberDto> InviteMemberAsync(Guid groupId, InviteMemberDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} inviting {TargetUserId} to group {GroupId}", currentUserId, dto.UserId, groupId);

        if (!await IsGroupLeaderAsync(currentUserId, groupId))
        {
            throw ErrorHelper.Forbidden("Only the group leader has the right to invite members.");
        }

        var targetUser = await _unitOfWork.Users.GetByIdAsync(dto.UserId);
        if (targetUser == null)
        {
            throw ErrorHelper.NotFound("The user does not exist.");
        }

        var existingMember = await _unitOfWork.GroupMembers.FirstOrDefaultAsync(gm =>
            gm.GroupId == groupId && gm.UserId == dto.UserId);

        if (existingMember != null)
        {
            if (existingMember.Status == GroupMemberStatus.Active)
            {
                throw ErrorHelper.Conflict("The user is already a member of the group.");
            }
            if (existingMember.Status == GroupMemberStatus.Pending)
            {
                throw ErrorHelper.Conflict("The invitation had been sent out earlier.");
            }
        }

        var isFriend = await _unitOfWork.Friendships.FirstOrDefaultAsync(f =>
            ((f.RequesterId == currentUserId && f.AddresseeId == dto.UserId) ||
             (f.RequesterId == dto.UserId && f.AddresseeId == currentUserId)) &&
            f.Status == FriendshipStatus.Accepted);

        if (isFriend == null)
        {
            throw ErrorHelper.Forbidden("You can only invite friends to the group.");
        }

        var invitation = new GroupMember
        {
            GroupId = groupId,
            UserId = dto.UserId,
            Role = GroupMemberRole.Member,
            Status = GroupMemberStatus.Pending
        };

        await _unitOfWork.GroupMembers.AddAsync(invitation);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Invitation sent to {TargetUserId} for group {GroupId}", dto.UserId, groupId);

        return new GroupMemberDto
        {
            UserId = targetUser.Id,
            Username = targetUser.Username,
            Email = targetUser.Email,
            AvatarUrl = targetUser.AvatarUrl,
            Role = GroupMemberRole.Member.ToString(),
            Status = GroupMemberStatus.Pending.ToString()
        };
    }

    public async Task<GroupMemberDto> AcceptInvitationAsync(Guid groupId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} accepting invitation to group {GroupId}", currentUserId, groupId);

        var invitation = await _unitOfWork.GroupMembers.FirstOrDefaultAsync(
            gm => gm.GroupId == groupId && gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Pending,
            gm => gm.User
        );

        if (invitation == null)
        {
            throw ErrorHelper.NotFound("The invitation does not exist or has already been processed.");
        }

        invitation.Status = GroupMemberStatus.Active;
        await _unitOfWork.GroupMembers.Update(invitation);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("User {CurrentUserId} joined group {GroupId}", currentUserId, groupId);

        return new GroupMemberDto
        {
            UserId = invitation.User.Id,
            Username = invitation.User.Username,
            Email = invitation.User.Email,
            AvatarUrl = invitation.User.AvatarUrl,
            Role = invitation.Role.ToString(),
            Status = invitation.Status.ToString()
        };
    }

    public async Task<bool> RejectInvitationAsync(Guid groupId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} rejecting invitation to group {GroupId}", currentUserId, groupId);

        var deleted = await _unitOfWork.GroupMembers.HardRemove(gm =>
            gm.GroupId == groupId && gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Pending);

        if (!deleted)
        {
            throw ErrorHelper.NotFound("The invitation does not exist.");
        }

        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Invitation rejected for group {GroupId}", groupId);

        return true;
    }

    public async Task<bool> RemoveMemberAsync(Guid groupId, Guid userId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} removing {TargetUserId} from group {GroupId}", currentUserId, userId, groupId);

        if (!await IsGroupLeaderAsync(currentUserId, groupId))
        {
            throw ErrorHelper.Forbidden("Only the team leader has the right to remove members.");
        }

        if (currentUserId == userId)
        {
            throw ErrorHelper.BadRequest("You cannot remove yourself from the group. Please use the leave group function.");
        }

        var deleted = await _unitOfWork.GroupMembers.HardRemove(gm =>
            gm.GroupId == groupId && gm.UserId == userId);

        if (!deleted)
        {
            throw ErrorHelper.NotFound("The member does not exist in the group.");
        }

        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("User {TargetUserId} removed from group {GroupId}", userId, groupId);

        return true;
    }

    public async Task<GroupMemberDto> PromoteToLeaderAsync(Guid groupId, Guid userId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} promoting {TargetUserId} to leader in group {GroupId}", currentUserId, userId, groupId);

        if (!await IsGroupLeaderAsync(currentUserId, groupId))
        {
            throw ErrorHelper.Forbidden("Only the team leader has the authority to transfer permissions.");
        }

        var targetMember = await _unitOfWork.GroupMembers.FirstOrDefaultAsync(
            gm => gm.GroupId == groupId && gm.UserId == userId && gm.Status == GroupMemberStatus.Active,
            gm => gm.User
        );

        if (targetMember == null)
        {
            throw ErrorHelper.NotFound("The member does not exist in the group.");
        }

        if (targetMember.Role == GroupMemberRole.Leader)
        {
            throw ErrorHelper.Conflict("The user is already a team leader.");
        }

        targetMember.Role = GroupMemberRole.Leader;
        await _unitOfWork.GroupMembers.Update(targetMember);

        var currentLeader = await _unitOfWork.GroupMembers.FirstOrDefaultAsync(gm =>
            gm.GroupId == groupId && gm.UserId == currentUserId);

        if (currentLeader != null)
        {
            currentLeader.Role = GroupMemberRole.Member;
            await _unitOfWork.GroupMembers.Update(currentLeader);
        }

        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("User {TargetUserId} promoted to leader in group {GroupId}", userId, groupId);

        return new GroupMemberDto
        {
            UserId = targetMember.User.Id,
            Username = targetMember.User.Username,
            Email = targetMember.User.Email,
            AvatarUrl = targetMember.User.AvatarUrl,
            Role = targetMember.Role.ToString(),
            Status = targetMember.Status.ToString()
        };
    }

    public async Task<bool> LeaveGroupAsync(Guid groupId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} leaving group {GroupId}", currentUserId, groupId);

        var member = await _unitOfWork.GroupMembers.FirstOrDefaultAsync(gm =>
            gm.GroupId == groupId && gm.UserId == currentUserId);

        if (member == null)
        {
            throw ErrorHelper.NotFound("You are not a member of this group.");
        }

        if (member.Role == GroupMemberRole.Leader)
        {
            var otherMembers = await _unitOfWork.GroupMembers.GetQueryable()
                .CountAsync(gm => gm.GroupId == groupId && gm.UserId != currentUserId && gm.Status == GroupMemberStatus.Active);

            if (otherMembers > 0)
            {
                throw ErrorHelper.Forbidden("The team leader needs to transfer authority before leaving the team.");
            }
        }

        await _unitOfWork.GroupMembers.HardRemove(gm => gm.GroupId == groupId && gm.UserId == currentUserId);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("User {CurrentUserId} left group {GroupId}", currentUserId, groupId);

        return true;
    }

    private async Task<bool> IsGroupLeaderAsync(Guid userId, Guid groupId)
    {
        var membership = await _unitOfWork.GroupMembers.FirstOrDefaultAsync(gm =>
            gm.UserId == userId &&
            gm.GroupId == groupId &&
            gm.Role == GroupMemberRole.Leader &&
            gm.Status == GroupMemberStatus.Active);

        return membership != null;
    }

    private async Task<bool> IsMemberAsync(Guid userId, Guid groupId)
    {
        var membership = await _unitOfWork.GroupMembers.FirstOrDefaultAsync(gm =>
            gm.UserId == userId &&
            gm.GroupId == groupId &&
            gm.Status == GroupMemberStatus.Active);

        return membership != null;
    }
}