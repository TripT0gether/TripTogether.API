using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TripTogether.Application.DTOs.GroupDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Services;

public sealed class GroupMemberService : IGroupMemberService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly ILogger _loggerService;

    public GroupMemberService(
        IUnitOfWork unitOfWork,
        IClaimsService claimsService,
        ILogger<GroupMemberService> loggerService)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _loggerService = loggerService;
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
            ((f.CreatedBy == currentUserId && f.AddresseeId == dto.UserId) ||
             (f.CreatedBy == dto.UserId && f.AddresseeId == currentUserId)) &&
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
}
