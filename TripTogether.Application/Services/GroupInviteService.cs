using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using TripTogether.Application.DTOs.GroupDTO;
using TripTogether.Application.DTOs.GroupInviteDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Services;

public sealed class GroupInviteService : IGroupInviteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly ILogger _loggerService;
    private readonly string _baseUrl;

    public GroupInviteService(
        IUnitOfWork unitOfWork,
        IClaimsService claimsService,
        ILogger<GroupInviteService> loggerService,
        IConfiguration configuration)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _loggerService = loggerService;
        _baseUrl = configuration["App:BaseUrl"] ?? "https://api.triptogether.com";
    }

    public async Task<GroupInviteDto> CreateInviteAsync(CreateGroupInviteDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} creating invite for group {GroupId}", currentUserId, dto.GroupId);

        var group = await _unitOfWork.Groups.GetQueryable()
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == dto.GroupId && !g.IsDeleted);

        if (group == null)
        {
            throw ErrorHelper.NotFound("The group does not exist.");
        }

        var isGroupMember = group.Members.Any(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to create an invite.");
        }

        var existingInvite = await _unitOfWork.GroupInvites.GetQueryable()
            .FirstOrDefaultAsync(i => i.GroupId == dto.GroupId && i.ExpiresAt > DateTime.UtcNow && !i.IsDeleted);

        if (existingInvite != null)
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

        _loggerService.LogInformation("Invite created successfully with token: {TokenPrefix}...", token[..8]);

        return new GroupInviteDto
        {
            Id = invite.Id,
            GroupId = invite.GroupId,
            GroupName = group.Name,
            Token = invite.Token,
            InviteUrl = BuildInviteUrl(invite.Token),
            ExpiresAt = invite.ExpiresAt,
            IsExpired = false,
            CreatedAt = invite.CreatedAt
        };
    }

    public async Task<GroupInviteDto> RefreshInviteAsync(Guid inviteId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} refreshing invite {InviteId}", currentUserId, inviteId);

        var invite = await _unitOfWork.GroupInvites.GetQueryable()
            .Include(i => i.Group)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(i => i.Id == inviteId && !i.IsDeleted);

        if (invite == null)
        {
            throw ErrorHelper.NotFound("The invite does not exist.");
        }

        var isGroupMember = invite.Group.Members.Any(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to refresh this invite.");
        }

        invite.Token = GenerateSecureToken();
        invite.ExpiresAt = DateTime.UtcNow.AddHours(24);
        invite.UpdatedAt = DateTime.UtcNow;
        invite.UpdatedBy = currentUserId;

        await _unitOfWork.GroupInvites.Update(invite);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Invite {InviteId} refreshed successfully to expire at {ExpiresAt}", inviteId, invite.ExpiresAt);

        return new GroupInviteDto
        {
            Id = invite.Id,
            GroupId = invite.GroupId,
            GroupName = invite.Group.Name,
            Token = invite.Token,
            InviteUrl = BuildInviteUrl(invite.Token),
            ExpiresAt = invite.ExpiresAt,
            IsExpired = false,
            CreatedAt = invite.CreatedAt
        };
    }


    public async Task<bool> RevokeInviteAsync(Guid inviteId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} revoking invite {InviteId}", currentUserId, inviteId);

        var invite = await _unitOfWork.GroupInvites.GetQueryable()
            .Include(i => i.Group)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(i => i.Id == inviteId && !i.IsDeleted);

        if (invite == null)
        {
            throw ErrorHelper.NotFound("The invite does not exist.");
        }

        var isGroupMember = invite.Group.Members.Any(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to revoke this invite.");
        }

        await _unitOfWork.GroupInvites.SoftRemove(invite);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("User {UserId} revoked invite {InviteId}", currentUserId, inviteId);

        return true;
    }


    public async Task<GroupInviteDto?> GetActiveInviteAsync(Guid groupId)
    {
        var invite = await _unitOfWork.GroupInvites.GetQueryable()
            .Include(i => i.Group)
            .Where(i => i.GroupId == groupId && i.ExpiresAt > DateTime.UtcNow && !i.IsDeleted)
            .FirstOrDefaultAsync();

        if (invite == null)
        {
            return null;
        }

        return new GroupInviteDto
        {
            Id = invite.Id,
            GroupId = invite.GroupId,
            GroupName = invite.Group.Name,
            Token = invite.Token,
            InviteUrl = BuildInviteUrl(invite.Token),
            ExpiresAt = invite.ExpiresAt,
            IsExpired = false,
            CreatedAt = invite.CreatedAt
        };
    }

    public async Task<GroupInviteDto> GetInviteByTokenAsync(string token)
    {
        var invite = await _unitOfWork.GroupInvites.GetQueryable()
            .Include(i => i.Group)
            .FirstOrDefaultAsync(i => i.Token == token && !i.IsDeleted);

        if (invite == null)
        {
            throw ErrorHelper.NotFound("The invite does not exist or has been revoked.");
        }

        return new GroupInviteDto
        {
            Id = invite.Id,
            GroupId = invite.GroupId,
            GroupName = invite.Group.Name,
            Token = invite.Token,
            InviteUrl = BuildInviteUrl(invite.Token),
            ExpiresAt = invite.ExpiresAt,
            IsExpired = invite.ExpiresAt <= DateTime.UtcNow,
            CreatedAt = invite.CreatedAt
        };
    }

    public async Task<GroupDto> JoinGroupByTokenAsync(string token)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} joining group with token", currentUserId);

        var invite = await _unitOfWork.GroupInvites.GetQueryable()
            .Include(i => i.Group)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(i => i.Token == token && !i.IsDeleted);

        if (invite == null)
        {
            throw ErrorHelper.NotFound("The invite does not exist or has been revoked.");
        }

        if (invite.ExpiresAt <= DateTime.UtcNow)
        {
            throw ErrorHelper.BadRequest("The invite has expired. Please request a new invite link.");
        }

        var group = invite.Group;

        var existingMember = group.Members.FirstOrDefault(m => m.UserId == currentUserId);

        if (existingMember != null)
        {
            if (existingMember.Status == GroupMemberStatus.Active)
            {
                throw ErrorHelper.Conflict("You are already a member of this group.");
            }
            if (existingMember.Status == GroupMemberStatus.Pending)
            {
                existingMember.Status = GroupMemberStatus.Active;
                existingMember.UpdatedAt = DateTime.UtcNow;
                existingMember.UpdatedBy = currentUserId;
                await _unitOfWork.GroupMembers.Update(existingMember);
            }
        }
        else
        {
            var newMember = new GroupMember
            {
                GroupId = group.Id,
                UserId = currentUserId,
                Role = GroupMemberRole.Member,
                Status = GroupMemberStatus.Active,
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow
            };

            await _unitOfWork.GroupMembers.AddAsync(newMember);
        }

        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("User {UserId} joined group {GroupId} successfully", currentUserId, group.Id);

        var memberCount = await _unitOfWork.GroupMembers.GetQueryable()
            .CountAsync(gm => gm.GroupId == group.Id && gm.Status == GroupMemberStatus.Active);

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

    private string BuildInviteUrl(string token)
    {
        return $"{_baseUrl}/api/groups/join?token={token}";
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
    }
}
