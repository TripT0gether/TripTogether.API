using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TripTogether.Application.DTOs.FriendshipDTO;
using TripTogether.Application.Interfaces;
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

        _loggerService.LogInformation("User {CurrentUserId} sending friend request to {AddresseeId}", currentUserId, dto.AddresseeId);

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
            (f.CreatedBy == currentUserId && f.AddresseeId == dto.AddresseeId) ||
            (f.CreatedBy == dto.AddresseeId && f.AddresseeId == currentUserId));

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

        var requester = await _unitOfWork.Users.GetByIdAsync(currentUserId);
        var friendship = new Friendship
        {
            AddresseeId = dto.AddresseeId,
            Status = FriendshipStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            CreatedBy = currentUserId,
            Requester = requester!,
            Addressee = addressee
        };

        await _unitOfWork.Friendships.AddAsync(friendship);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Friend request sent successfully from {CurrentUserId} to {AddresseeId}", currentUserId, dto.AddresseeId);

        return MapToDto(friendship);
    }

    public async Task<FriendshipDto> AcceptFriendRequestAsync(Guid friendshipId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} accepting friendship {FriendshipId}", currentUserId, friendshipId);

        var friendship = await _unitOfWork.Friendships.FirstOrDefaultAsync(
            f => f.Id == friendshipId && f.Status == FriendshipStatus.Pending,
            f => f.Requester,
            f => f.Addressee
        );

        if (friendship == null)
        {
            throw ErrorHelper.NotFound("The friend request does not exist or has already been processed.");
        }

        if (friendship.AddresseeId != currentUserId)
        {
            throw ErrorHelper.Forbidden("You can only accept friend requests sent to you.");
        }

        friendship.Status = FriendshipStatus.Accepted;
        friendship.UpdatedAt = DateTime.UtcNow;
        friendship.UpdatedBy = currentUserId;

        await _unitOfWork.Friendships.Update(friendship);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Friendship {FriendshipId} accepted successfully", friendshipId);

        return MapToDto(friendship);
    }

    public async Task<bool> RejectFriendRequestAsync(Guid friendshipId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} rejecting friendship {FriendshipId}", currentUserId, friendshipId);

        var friendship = await _unitOfWork.Friendships.FirstOrDefaultAsync(
            f => f.Id == friendshipId && f.Status == FriendshipStatus.Pending
        );

        if (friendship == null)
        {
            throw ErrorHelper.NotFound("The friend request does not exist.");
        }

        if (friendship.AddresseeId != currentUserId)
        {
            throw ErrorHelper.Forbidden("You can only reject friend requests sent to you.");
        }

        var deleted = await _unitOfWork.Friendships.HardRemove(f => f.Id == friendshipId);

        if (!deleted)
        {
            throw ErrorHelper.NotFound("Failed to reject the friend request.");
        }

        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Friendship {FriendshipId} rejected successfully", friendshipId);

        return true;
    }

    public async Task<bool> UnfriendAsync(Guid friendshipId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} unfriending via friendship {FriendshipId}", currentUserId, friendshipId);

        var friendship = await _unitOfWork.Friendships.FirstOrDefaultAsync(
            f => f.Id == friendshipId && f.Status == FriendshipStatus.Accepted,
            f => f.Requester,
            f => f.Addressee
        );

        if (friendship == null)
        {
            _loggerService.LogWarning("Friendship {FriendshipId} not found or not accepted for user {CurrentUserId}", friendshipId, currentUserId);
            throw ErrorHelper.NotFound("The friendship does not exist.");
        }

        if (friendship.CreatedBy != currentUserId && friendship.AddresseeId != currentUserId)
        {
            throw ErrorHelper.Forbidden("You can only unfriend your own friendships.");
        }

        var deleted = await _unitOfWork.Friendships.HardRemove(f => f.Id == friendshipId);

        if (!deleted)
        {
            throw ErrorHelper.NotFound("Failed to unfriend.");
        }

        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Friendship {FriendshipId} removed successfully", friendshipId);

        return true;
    }

    public async Task<Pagination<FriendListDto>> GetFriendsListAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string? sortBy = null,
        bool ascending = true)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("Getting friends list for user {CurrentUserId}", currentUserId);

        IQueryable<Friendship> friendshipsQuery = _unitOfWork.Friendships.GetQueryable()
            .Where(f => ((f.CreatedBy == currentUserId || f.AddresseeId == currentUserId) &&
                  f.Status == FriendshipStatus.Accepted))
            .Include(f => f.Requester)
            .Include(f => f.Addressee);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            friendshipsQuery = friendshipsQuery.Where(f =>
                (f.CreatedBy == currentUserId &&
                 (f.Addressee.Username.ToLower().Contains(lowerSearchTerm) ||
                  f.Addressee.Email.ToLower().Contains(lowerSearchTerm))) ||
                (f.AddresseeId == currentUserId &&
                 (f.Requester.Username.ToLower().Contains(lowerSearchTerm) ||
                  f.Requester.Email.ToLower().Contains(lowerSearchTerm))));
        }

        var totalCount = await friendshipsQuery.CountAsync();

        friendshipsQuery = ascending
            ? friendshipsQuery.OrderBy(f => f.CreatedAt)
            : friendshipsQuery.OrderByDescending(f => f.CreatedAt);

        var friendships = await friendshipsQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var friendsList = friendships.Select(f =>
        {
            var friend = f.CreatedBy == currentUserId ? f.Addressee : f.Requester;
            return new FriendListDto
            {
                FriendshipId = f.Id,
                Username = friend.Username,
                Email = friend.Email,
                AvatarUrl = friend.AvatarUrl,
                FriendsSince = f.CreatedAt
            };
        }).ToList();

        _loggerService.LogInformation("Found {FriendsCount} friends for user {CurrentUserId} on page {PageNumber}", friendsList.Count, currentUserId, pageNumber);

        return new Pagination<FriendListDto>(friendsList, totalCount, pageNumber, pageSize);
    }

    public async Task<Pagination<FriendRequestDto>> GetFriendRequestsAsync(
        FriendRequestType type,
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("Getting {Type} friend requests for user {CurrentUserId}", type, currentUserId);

        IQueryable<Friendship> requestsQuery = type == FriendRequestType.Received
            ? _unitOfWork.Friendships.GetQueryable()
                .Where(f => f.AddresseeId == currentUserId && f.Status == FriendshipStatus.Pending)
                .Include(f => f.Requester)
            : _unitOfWork.Friendships.GetQueryable()
                .Where(f => f.CreatedBy == currentUserId && f.Status == FriendshipStatus.Pending)
                .Include(f => f.Addressee);

        if (!string.IsNullOrWhiteSpace(searchTerm))
        {
            var lowerSearchTerm = searchTerm.ToLower();
            requestsQuery = type == FriendRequestType.Received
                ? requestsQuery.Where(f => f.Requester.Username.ToLower().Contains(lowerSearchTerm))
                : requestsQuery.Where(f => f.Addressee.Username.ToLower().Contains(lowerSearchTerm));
        }

        var totalCount = await requestsQuery.CountAsync();

        requestsQuery = requestsQuery.OrderByDescending(f => f.CreatedAt);

        var requests = await requestsQuery
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var requestDtos = requests.Select(f =>
        {
            var user = type == FriendRequestType.Received ? f.Requester : f.Addressee;
            return new FriendRequestDto
            {
                FriendshipId = f.Id,
                UserId = user.Id,
                Username = user.Username,
                AvatarUrl = user.AvatarUrl,
                RequestDate = f.CreatedAt
            };
        }).ToList();

        return new Pagination<FriendRequestDto>(requestDtos, totalCount, pageNumber, pageSize);
    }

    public async Task<Pagination<UserSearchResultDto>> SearchUsersAsync(SearchUsersDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} searching for users with term: {SearchTerm}", currentUserId, dto.SearchTerm);

        if (string.IsNullOrWhiteSpace(dto.SearchTerm))
        {
            throw ErrorHelper.BadRequest("Search term cannot be empty.");
        }

        var lowerSearchTerm = dto.SearchTerm.ToLower();

        // Get all existing friendships (accepted, pending, blocked)
        var existingFriendshipUserIds = await _unitOfWork.Friendships.GetQueryable()
            .Where(f => f.CreatedBy == currentUserId || f.AddresseeId == currentUserId)
            .Select(f => f.CreatedBy == currentUserId ? f.AddresseeId : f.CreatedBy)
            .ToListAsync();

        // Search for users by username or email, excluding current user and existing friendships
        var usersQuery = _unitOfWork.Users.GetQueryable()
            .Where(u => u.Id != currentUserId &&
                        !existingFriendshipUserIds.Contains(u.Id) &&
                        !u.IsDeleted &&
                        u.IsEmailVerified &&
                        (u.Username.ToLower().Contains(lowerSearchTerm) ||
                         u.Email.ToLower().Contains(lowerSearchTerm)));

        var totalCount = await usersQuery.CountAsync();

        var users = await usersQuery
            .OrderBy(u => u.Username)
            .Skip((dto.PageNumber - 1) * dto.PageSize)
            .Take(dto.PageSize)
            .ToListAsync();

        var userSearchResults = users.Select(u => new UserSearchResultDto
        {
            Id = u.Id,
            Username = u.Username,
            Email = u.Email,
            AvatarUrl = u.AvatarUrl,
            Gender = u.Gender,
            IsEmailVerified = u.IsEmailVerified
        }).ToList();

        _loggerService.LogInformation("Found {UsersCount} users matching search term '{SearchTerm}' for user {CurrentUserId}", 
            userSearchResults.Count, dto.SearchTerm, currentUserId);

        return new Pagination<UserSearchResultDto>(userSearchResults, totalCount, dto.PageNumber, dto.PageSize);
    }

    private static FriendshipDto MapToDto(Friendship friendship)
    {
        return new FriendshipDto
        {
            Id = friendship.Id,
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