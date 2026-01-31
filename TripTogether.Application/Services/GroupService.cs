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

        _loggerService.LogInformation($"User {currentUserId} creating group: {dto.Name}");

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

        _loggerService.LogInformation($"Group {group.Id} created successfully by user {currentUserId}");

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

        _loggerService.LogInformation($"User {currentUserId} updating group {groupId}");

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

        _loggerService.LogInformation($"Group {groupId} updated successfully");

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

        _loggerService.LogInformation($"User {currentUserId} uploading cover photo for group {groupId}");

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

        _loggerService.LogInformation($"Cover photo uploaded successfully for group {groupId}");

        return coverPhotoUrl;
    }

    public async Task<bool> DeleteGroupAsync(Guid groupId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} deleting group {groupId}");

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

        _loggerService.LogInformation($"Group {groupId} deleted successfully");

        return true;
    }

    public async Task<GroupDetailDto> GetGroupDetailAsync(Guid groupId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} getting details for group {groupId}");

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

    public async Task<Pagination<GroupDto>> GetMyGroupsAsync(int pageNumber = 1, int pageSize = 10)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"Getting groups for user {currentUserId}");

        var groupMembersQuery = _unitOfWork.GroupMembers.GetQueryable()
            .Where(gm => gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Active)
            .Include(gm => gm.Group);

        var totalCount = await groupMembersQuery.CountAsync();

        var groupMembers = await groupMembersQuery
            .OrderByDescending(gm => gm.Group.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var groups = new List<GroupDto>();

        foreach (var gm in groupMembers)
        {
            var memberCount = await _unitOfWork.GroupMembers.GetQueryable()
                .CountAsync(m => m.GroupId == gm.GroupId && m.Status == GroupMemberStatus.Active);

            groups.Add(new GroupDto
            {
                Id = gm.Group.Id,
                Name = gm.Group.Name,
                CoverPhotoUrl = gm.Group.CoverPhotoUrl,
                CreatedBy = gm.Group.CreatedBy,
                CreatedAt = gm.Group.CreatedAt,
                MemberCount = memberCount
            });
        }

        return new Pagination<GroupDto>(groups, totalCount, pageNumber, pageSize);
    }

    public async Task<GroupDto> JoinGroupByToken(string token)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} joining group with token");

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

        _loggerService.LogInformation($"User {currentUserId} joined group {group.Id} successfully");

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

        _loggerService.LogInformation($"User {currentUserId} inviting {dto.UserId} to group {groupId}");

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

        // Tạo lời mời
        var invitation = new GroupMember
        {
            GroupId = groupId,
            UserId = dto.UserId,
            Role = GroupMemberRole.Member,
            Status = GroupMemberStatus.Pending
        };

        await _unitOfWork.GroupMembers.AddAsync(invitation);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Invitation sent to {dto.UserId} for group {groupId}");

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

        _loggerService.LogInformation($"User {currentUserId} accepting invitation to group {groupId}");

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

        _loggerService.LogInformation($"User {currentUserId} joined group {groupId}");

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

        _loggerService.LogInformation($"User {currentUserId} rejecting invitation to group {groupId}");

        var deleted = await _unitOfWork.GroupMembers.HardRemove(gm =>
            gm.GroupId == groupId && gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Pending);

        if (!deleted)
        {
            throw ErrorHelper.NotFound("The invitation does not exist.");
        }

        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Invitation rejected for group {groupId}");

        return true;
    }

    public async Task<bool> RemoveMemberAsync(Guid groupId, Guid userId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} removing {userId} from group {groupId}");

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

        _loggerService.LogInformation($"User {userId} removed from group {groupId}");

        return true;
    }

    public async Task<GroupMemberDto> PromoteToLeaderAsync(Guid groupId, Guid userId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} promoting {userId} to leader in group {groupId}");

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

        _loggerService.LogInformation($"User {userId} promoted to leader in group {groupId}");

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

        _loggerService.LogInformation($"User {currentUserId} leaving group {groupId}");

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

        _loggerService.LogInformation($"User {currentUserId} left group {groupId}");

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