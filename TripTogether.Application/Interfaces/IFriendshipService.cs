using TripTogether.Application.DTOs.FriendshipDTO;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Interfaces;

public interface IFriendshipService
{
    Task<FriendshipDto> SendFriendRequestAsync(SendFriendRequestDto dto);

    Task<FriendshipDto> AcceptFriendRequestAsync(Guid friendshipId);

    Task<bool> RejectFriendRequestAsync(Guid friendshipId);

    Task<bool> UnfriendAsync(Guid friendshipId);

    Task<Pagination<FriendListDto>> GetFriendsListAsync(
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null,
        string? sortBy = null,
        bool ascending = true);

    Task<Pagination<FriendRequestDto>> GetFriendRequestsAsync(
        FriendRequestType type,
        int pageNumber = 1,
        int pageSize = 10,
        string? searchTerm = null);

    Task<Pagination<UserSearchResultDto>> SearchUsersAsync(SearchUsersDto dto);
}