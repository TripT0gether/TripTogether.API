using TripTogether.Application.DTOs.GroupInviteDTO;

namespace TripTogether.Application.Interfaces;

public interface IGroupInviteService
{
    Task<GroupInviteDto> CreateInviteAsync(CreateGroupInviteDto dto);
    Task<GroupInviteDto> RefreshInviteAsync(Guid inviteId);
    Task<bool> ValidateInviteTokenAsync(string token);
    Task<bool> RevokeInviteAsync(Guid inviteId);
    Task<List<GroupInviteDto>> GetGroupInvitesAsync(Guid groupId);
}
