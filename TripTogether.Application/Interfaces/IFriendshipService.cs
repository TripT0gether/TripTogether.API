using TripTogether.Application.DTOs.FriendshipDTO;

namespace TripTogether.Application.Interfaces
{
    public interface IFriendshipService
    {
        Task<FriendshipDto> SendFriendRequestAsync(SendFriendRequestDto dto);

        Task<FriendshipDto> AcceptFriendRequestAsync(Guid requesterId);

        Task<bool> RejectFriendRequestAsync(Guid requesterId);

        Task<bool> UnfriendAsync(Guid friendId);

        Task<List<FriendListDto>> GetFriendsListAsync();

        Task<List<FriendshipDto>> GetPendingRequestsAsync();

        Task<List<FriendshipDto>> GetSentRequestsAsync();
    }
}