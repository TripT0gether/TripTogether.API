using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using TripTogether.Application.DTOs.TripInviteDTO;
using TripTogether.Application.Interfaces;

namespace TripTogether.Application.Services;

public sealed class TripInviteService : ITripInviteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly ILogger _loggerService;

    public TripInviteService(
        IUnitOfWork unitOfWork,
        IClaimsService claimsService,
        ILogger<TripInviteService> loggerService)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _loggerService = loggerService;
    }

    public async Task<TripInviteDto> CreateInviteAsync(CreateTripInviteDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} creating invite for trip {dto.TripId}");

        var trip = await _unitOfWork.Trips.GetQueryable()
            .Include(t => t.Group)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(t => t.Id == dto.TripId);

        if (trip == null)
        {
            throw ErrorHelper.NotFound("The trip does not exist.");
        }

        var isGroupMember = trip.Group.Members.Any(m => m.UserId == currentUserId && m.Status == Domain.Enums.GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to create an invite.");
        }

        var token = GenerateSecureToken();
        var expiresAt = DateTime.UtcNow.AddHours(dto.ExpiresInHours);

        var invite = new TripInvite
        {
            TripId = dto.TripId,
            Token = token,
            ExpiresAt = expiresAt,
            CreatedBy = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.TripInvites.AddAsync(invite);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Invite created successfully with token: {token.Substring(0, 8)}...");

        return new TripInviteDto
        {
            Id = invite.Id,
            TripId = invite.TripId,
            TripTitle = trip.Title,
            Token = invite.Token,
            ExpiresAt = invite.ExpiresAt,
            IsExpired = invite.ExpiresAt <= DateTime.UtcNow,
            CreatedAt = invite.CreatedAt
        };
    }


    public async Task<bool> ValidateInviteTokenAsync(string token)
    {
        var invite = await _unitOfWork.TripInvites.GetQueryable()
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

        var invite = await _unitOfWork.TripInvites.GetQueryable()
            .Include(i => i.Trip)
            .ThenInclude(t => t.Group)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(i => i.Id == inviteId);

        if (invite == null)
        {
            throw ErrorHelper.NotFound("The invite does not exist.");
        }

        var isGroupMember = invite.Trip.Group.Members.Any(m => m.UserId == currentUserId && m.Status == Domain.Enums.GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to revoke this invite.");
        }

        await _unitOfWork.TripInvites.SoftRemove(invite);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"User {currentUserId} revoked invite {inviteId}");

        return true;
    }

    public async Task<List<TripInviteDto>> GetTripInvitesAsync(Guid tripId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        var trip = await _unitOfWork.Trips.GetQueryable()
            .Include(t => t.Group)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(t => t.Id == tripId);

        if (trip == null)
        {
            throw ErrorHelper.NotFound("The trip does not exist.");
        }

        var isGroupMember = trip.Group.Members.Any(m => m.UserId == currentUserId && m.Status == Domain.Enums.GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to view invites.");
        }

        var invites = await _unitOfWork.TripInvites.GetQueryable()
            .Include(i => i.Trip)
            .Where(i => i.TripId == tripId)
            .OrderByDescending(i => i.CreatedAt)
            .ToListAsync();

        return invites.Select(i => new TripInviteDto
        {
            Id = i.Id,
            TripId = i.TripId,
            TripTitle = i.Trip.Title,
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
