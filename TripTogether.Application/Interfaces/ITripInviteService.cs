using TripTogether.Application.DTOs.TripInviteDTO;

namespace TripTogether.Application.Interfaces;

public interface ITripInviteService
{
    Task<TripInviteDto> CreateInviteAsync(CreateTripInviteDto dto);
    Task<bool> ValidateInviteTokenAsync(string token);
    Task<bool> RevokeInviteAsync(Guid inviteId);
    Task<List<TripInviteDto>> GetTripInvitesAsync(Guid tripId);
}
