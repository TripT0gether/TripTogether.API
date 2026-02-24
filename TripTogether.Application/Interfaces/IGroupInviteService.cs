using TripTogether.Application.DTOs.GroupDTO;
using TripTogether.Application.DTOs.GroupInviteDTO;

namespace TripTogether.Application.Interfaces;

public interface IGroupInviteService
{
    Task<GroupInviteDto> CreateInviteAsync(CreateGroupInviteDto dto);
    Task<GroupInviteDto> RefreshInviteAsync(Guid inviteId);
    Task<bool> RevokeInviteAsync(Guid inviteId);
    Task<GroupInviteDto?> GetActiveInviteAsync(Guid groupId);
    Task<GroupInviteDto> GetInviteByTokenAsync(string token);
    Task<GroupDto> JoinGroupByTokenAsync(string token);
}
