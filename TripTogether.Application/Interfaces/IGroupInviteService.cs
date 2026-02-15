using TripTogether.Application.DTOs.GroupDTO;
using TripTogether.Application.DTOs.GroupInviteDTO;

namespace TripTogether.Application.Interfaces;

public interface IGroupInviteService
{
    Task<GroupInviteDto> CreateInviteAsync(CreateGroupInviteDto dto);
    Task<GroupInviteDto> RefreshInviteAsync(Guid inviteId);
    Task<bool> ValidateInviteTokenAsync(string token);
    Task<bool> RevokeInviteAsync(Guid inviteId);
    Task<List<GroupInviteDto>> GetGroupInvitesAsync(Guid groupId);
    Task<GroupInviteDto?> GetActiveInviteAsync(Guid groupId);
    Task<GroupDto> JoinGroupByTokenAsync(string token);
    Task<GroupInviteDto> GetInviteByTokenAsync(string token);
}
