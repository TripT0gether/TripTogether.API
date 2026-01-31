using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TripTogether.Application.DTOs.FriendshipDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Application.Utils;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Services;

public sealed class FriendshipService : IFriendshipService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly ILogger _loggerService;

    public FriendshipService(
        IUnitOfWork unitOfWork,
        IClaimsService claimsService,
        ILogger<FriendshipService> loggerService)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _loggerService = loggerService;
    }

    public async Task<FriendshipDto> SendFriendRequestAsync(SendFriendRequestDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} sending friend request to {dto.AddresseeId}");

        if (currentUserId == dto.AddresseeId)
        {
            throw ErrorHelper.BadRequest("Unable to send a friend request to myself.");
        }

        var addressee = await _unitOfWork.Users.GetByIdAsync(dto.AddresseeId);
        if (addressee == null)
        {
            throw ErrorHelper.NotFound("The user does not exist.");
        }

        var existingFriendship = await _unitOfWork.Friendships.FirstOrDefaultAsync(f =>
            (f.RequesterId == currentUserId && f.AddresseeId == dto.AddresseeId) ||
            (f.RequesterId == dto.AddresseeId && f.AddresseeId == currentUserId));

        if (existingFriendship != null)
        {
            if (existingFriendship.Status == FriendshipStatus.Accepted)
            {
                throw ErrorHelper.Conflict("You were friends with this person.");
            }
            if (existingFriendship.Status == FriendshipStatus.Pending)
            {
                throw ErrorHelper.Conflict("A friend request was sent previously.");
            }
            if (existingFriendship.Status == FriendshipStatus.Blocked)
            {
                throw ErrorHelper.Forbidden("Unable to send friend requests.");
            }
        }

        var friendship = new Friendship
        {
            RequesterId = currentUserId,
            AddresseeId = dto.AddresseeId,
            Status = FriendshipStatus.Pending,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Friendships.AddAsync(friendship);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Friend request sent successfully from {currentUserId} to {dto.AddresseeId}");

        var result = await _unitOfWork.Friendships.FirstOrDefaultAsync(
            f => f.RequesterId == currentUserId && f.AddresseeId == dto.AddresseeId,
            f => f.Requester,
            f => f.Addressee
        );

        return MapToDto(result!);
    }

    public async Task<FriendshipDto> AcceptFriendRequestAsync(Guid requesterId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} accepting friend request from {requesterId}");

        var friendship = await _unitOfWork.Friendships.FirstOrDefaultAsync(
            f => f.RequesterId == requesterId && f.AddresseeId == currentUserId && f.Status == FriendshipStatus.Pending,
            f => f.Requester,
            f => f.Addressee
        );

        if (friendship == null)
        {
            throw ErrorHelper.NotFound("The friend request does not exist or has already been processed.");
        }

        friendship.Status = FriendshipStatus.Accepted;
        await _unitOfWork.Friendships.Update(friendship);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Friend request accepted: {requesterId} and {currentUserId} are now friends");

        return MapToDto(friendship);
    }

    public async Task<bool> RejectFriendRequestAsync(Guid requesterId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} rejecting friend request from {requesterId}");

        var deleted = await _unitOfWork.Friendships.HardRemove(f =>
            f.RequesterId == requesterId && f.AddresseeId == currentUserId && f.Status == FriendshipStatus.Pending);

        if (!deleted)
        {
            throw ErrorHelper.NotFound("The friend request does not exist.");
        }

        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Friend request rejected from {requesterId} to {currentUserId}");

        return true;
    }

    public async Task<bool> UnfriendAsync(Guid friendId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} unfriending {friendId}");

        var deleted = await _unitOfWork.Friendships.HardRemove(f =>
            ((f.RequesterId == currentUserId && f.AddresseeId == friendId) ||
             (f.RequesterId == friendId && f.AddresseeId == currentUserId)) &&
            f.Status == FriendshipStatus.Accepted);

        if (!deleted)
        {
            throw ErrorHelper.NotFound("No friendships found.");
        }

        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Unfriended successfully: {currentUserId} and {friendId}");

        return true;
    }

    public async Task<Pagination<FriendListDto>> GetFriendsListAsync(int pageNumber = 1, int pageSize = 10)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"Getting friends list for user {currentUserId}");

        var friendshipsQuery = _unitOfWork.Friendships.GetQueryable()
            .Where(f => ((f.RequesterId == currentUserId || f.AddresseeId == currentUserId) &&
                  f.Status == FriendshipStatus.Accepted))
            .Include(f => f.Requester)
            .Include(f => f.Addressee);

        var totalCount = await friendshipsQuery.CountAsync();

        var friendships = await friendshipsQuery
            .OrderBy(f => f.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var friends = friendships.Select(f =>
        {
            var friend = f.RequesterId == currentUserId ? f.Addressee : f.Requester;
            return new FriendListDto
            {
                FriendId = friend.Id,
                Username = friend.Username,
                Email = friend.Email,
                AvatarUrl = friend.AvatarUrl,
                FriendsSince = f.CreatedAt
            };
        }).ToList();

        _loggerService.LogInformation($"Found {friends.Count} friends for user {currentUserId} on page {pageNumber}");

        return new Pagination<FriendListDto>(friends, totalCount, pageNumber, pageSize);
    }

    public async Task<Pagination<FriendshipDto>> GetPendingRequestsAsync(int pageNumber = 1, int pageSize = 10)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"Getting pending friend requests for user {currentUserId}");

        var requestsQuery = _unitOfWork.Friendships.GetQueryable()
            .Where(f => f.AddresseeId == currentUserId && f.Status == FriendshipStatus.Pending)
            .Include(f => f.Requester)
            .Include(f => f.Addressee);

        var totalCount = await requestsQuery.CountAsync();

        var requests = await requestsQuery
            .OrderByDescending(f => f.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var requestDtos = requests.Select(MapToDto).ToList();

        return new Pagination<FriendshipDto>(requestDtos, totalCount, pageNumber, pageSize);
    }

    public async Task<Pagination<FriendshipDto>> GetSentRequestsAsync(int pageNumber = 1, int pageSize = 10)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"Getting sent friend requests for user {currentUserId}");

        var requestsQuery = _unitOfWork.Friendships.GetQueryable()
            .Where(f => f.RequesterId == currentUserId && f.Status == FriendshipStatus.Pending)
            .Include(f => f.Requester)
            .Include(f => f.Addressee);

        var totalCount = await requestsQuery.CountAsync();

        var requests = await requestsQuery
            .OrderByDescending(f => f.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var requestDtos = requests.Select(MapToDto).ToList();

        return new Pagination<FriendshipDto>(requestDtos, totalCount, pageNumber, pageSize);
    }

    private static FriendshipDto MapToDto(Friendship friendship)
    {
        return new FriendshipDto
        {
            RequesterId = friendship.RequesterId,
            AddresseeId = friendship.AddresseeId,
            Status = friendship.Status.ToString(),
            CreatedAt = friendship.CreatedAt,
            Requester = new UserBasicInfoDto
            {
                Id = friendship.Requester.Id,
                Username = friendship.Requester.Username,
                Email = friendship.Requester.Email,
                AvatarUrl = friendship.Requester.AvatarUrl
            },
            Addressee = new UserBasicInfoDto
            {
                Id = friendship.Addressee.Id,
                Username = friendship.Addressee.Username,
                Email = friendship.Addressee.Email,
                AvatarUrl = friendship.Addressee.AvatarUrl
            }
        };
    }
}