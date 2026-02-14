using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using TripTogether.Application.DTOs.GroupInviteDTO;
using TripTogether.Application.Interfaces;

namespace TripTogether.Application.Services;

public sealed class GroupInviteService : IGroupInviteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly ILogger _loggerService;

    public GroupInviteService(
        IUnitOfWork unitOfWork,
        IClaimsService claimsService,
        ILogger<GroupInviteService> loggerService)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _loggerService = loggerService;
    }

    public async Task<GroupInviteDto> CreateInviteAsync(CreateGroupInviteDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} creating invite for group {dto.GroupId}");

        var group = await _unitOfWork.Groups.GetQueryable()
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == dto.GroupId);

        if (group == null)
        {
            throw ErrorHelper.NotFound("The group does not exist.");
        }

        var isGroupMember = group.Members.Any(m => m.UserId == currentUserId && m.Status == Domain.Enums.GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to create an invite.");
        }

        if (await _unitOfWork.GroupInvites.GetQueryable()
            .AnyAsync(i => i.GroupId == dto.GroupId && i.ExpiresAt > DateTime.UtcNow))
        {
            throw ErrorHelper.Conflict("An active invite already exists for this group.");
        }

        if (dto.ExpiresInHours <= 0 || dto.ExpiresInHours > 168)
        {
            throw ErrorHelper.BadRequest("Expiration time must be between 1 and 168 hours.");
        }

        var token = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddHours(dto.ExpiresInHours);

        var invite = new GroupInvite
        {
            GroupId = dto.GroupId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedBy = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.GroupInvites.AddAsync(invite);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Invite created successfully with token: {token.Substring(0, 8)}...");

        return new GroupInviteDto
        {
            Id = invite.Id,
            GroupId = invite.GroupId,
            GroupName = group.Name,
            Token = invite.Token,
            ExpiresAt = invite.ExpiresAt,
            IsExpired = invite.ExpiresAt <= DateTime.UtcNow,
            CreatedAt = invite.CreatedAt
        };
    }

    public async Task<GroupInviteDto> RefreshInviteAsync(Guid inviteId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} refreshing invite {inviteId}");

        var invite = await _unitOfWork.GroupInvites.GetQueryable()
            .Include(i => i.Group)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(i => i.Id == inviteId);

        if (invite == null)
        {
            throw ErrorHelper.NotFound("The invite does not exist.");
        }

        var isGroupMember = invite.Group.Members.Any(m => m.UserId == currentUserId && m.Status == Domain.Enums.GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to refresh this invite.");
        }

        invite.ExpiresAt = DateTime.UtcNow.AddHours(24);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Invite {inviteId} refreshed successfully to expire at {invite.ExpiresAt}");

        return new GroupInviteDto
        {
            Id = invite.Id,
            GroupId = invite.GroupId,
            GroupName = invite.Group.Name,
            Token = invite.Token,
            ExpiresAt = invite.ExpiresAt,
            IsExpired = invite.ExpiresAt <= DateTime.UtcNow,
            CreatedAt = invite.CreatedAt
        };
    }

    public async Task<bool> ValidateInviteTokenAsync(string token)
    {
        var invite = await _unitOfWork.GroupInvites.GetQueryable()
            .FirstOrDefaultAsync(i => i.Token == token);

        if (invite == null)
        {
            return false;
        }

        return invite.ExpiresAt > DateTime.UtcNow;
    }

    public async Task<bool> RevokeInviteAsync(Guid inviteId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        var invite = await _unitOfWork.GroupInvites.GetQueryable()
            .Include(i => i.Group)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(i => i.Id == inviteId);

        if (invite == null)
        {
            throw ErrorHelper.NotFound("The invite does not exist.");
        }

        var isGroupMember = invite.Group.Members.Any(m => m.UserId == currentUserId && m.Status == Domain.Enums.GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to revoke this invite.");
        }

        await _unitOfWork.GroupInvites.SoftRemove(invite);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"User {currentUserId} revoked invite {inviteId}");

        return true;
    }

    public async Task<List<GroupInviteDto>> GetGroupInvitesAsync(Guid groupId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        var group = await _unitOfWork.Groups.GetQueryable()
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId);

        if (group == null)
        {
            throw ErrorHelper.NotFound("The group does not exist.");
        }

        var isGroupMember = group.Members.Any(m => m.UserId == currentUserId && m.Status == Domain.Enums.GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to view invites.");
        }

        var invites = await _unitOfWork.GroupInvites.GetQueryable()
            .Include(i => i.Group)
            .Where(i => i.GroupId == groupId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invites.Select(i => new GroupInviteDto
        {
            Id = i.Id,
            GroupId = i.GroupId,
            GroupName = i.Group.Name,
            Token = i.Token,
            ExpiresAt = i.ExpiresAt,
            IsExpired = i.ExpiresAt <= DateTime.UtcNow,
            CreatedAt = i.CreatedAt
        }).ToList();
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
