using TripTogether.Application.DTOs.FriendshipDTO;
using TripTogether.Application.Utils;

namespace TripTogether.Application.Interfaces
{
    public interface IFriendshipService
    {
        Task<FriendshipDto> SendFriendRequestAsync(SendFriendRequestDto dto);

        Task<FriendshipDto> AcceptFriendRequestAsync(Guid requesterId);

        Task<bool> RejectFriendRequestAsync(Guid requesterId);

        Task<bool> UnfriendAsync(Guid friendId);

        Task<Pagination<FriendListDto>> GetFriendsListAsync(
            int pageNumber = 1, 
            int pageSize = 10, 
            string? searchTerm = null, 
            string? sortBy = null, 
            bool ascending = true);

        Task<Pagination<FriendshipDto>> GetPendingRequestsAsync(
            int pageNumber = 1, 
            int pageSize = 10, 
            string? searchTerm = null, 
            string? sortBy = null, 
            bool ascending = true);

        Task<Pagination<FriendshipDto>> GetSentRequestsAsync(
            int pageNumber = 1, 
            int pageSize = 10, 
            string? searchTerm = null, 
            string? sortBy = null, 
            bool ascending = true);
    }
}