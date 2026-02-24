using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TripTogether.Application.DTOs.GroupDTO;
using TripTogether.Application.DTOs.GroupInviteDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Services;

public sealed class GroupService : IGroupService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly IFileService _fileService;
    private readonly IGroupInviteService _groupInviteService;
    private readonly ILogger _loggerService;

    public GroupService(
        IUnitOfWork unitOfWork,
        IClaimsService claimsService,
        IFileService fileService,
        IGroupInviteService groupInviteService,
        ILogger<GroupService> loggerService)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _fileService = fileService;
        _groupInviteService = groupInviteService;
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

        var invite = new CreateGroupInviteDto
        {
            GroupId = group.Id,
            ExpiresInHours = 24
        };
        await _groupInviteService.CreateInviteAsync(invite);

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
        return await _fileService.UploadGroupCoverPhotoAsync(groupId, file);
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

        var activeInvite = await _groupInviteService.GetActiveInviteAsync(groupId);

        return new GroupDetailDto
        {
            Id = group.Id,
            Name = group.Name,
            CoverPhotoUrl = group.CoverPhotoUrl,
            CreatedBy = group.CreatedBy,
            CreatedAt = group.CreatedAt,
            InviteToken = activeInvite?.Token,
            InviteExpiresAt = activeInvite?.ExpiresAt,
            IsInviteExpired = activeInvite?.IsExpired,
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
        return await _groupInviteService.JoinGroupByTokenAsync(token);
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