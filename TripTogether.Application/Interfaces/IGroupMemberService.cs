using Microsoft.AspNetCore.Http;
using TripTogether.Application.DTOs.GroupDTO;

namespace TripTogether.Application.Interfaces;

public interface IGroupMemberService
{
    Task<GroupMemberDto> InviteMemberAsync(Guid groupId, InviteMemberDto dto);

    Task<GroupMemberDto> AcceptInvitationAsync(Guid groupId);

    Task<bool> RejectInvitationAsync(Guid groupId);

    Task<bool> RemoveMemberAsync(Guid groupId, Guid userId);

    Task<GroupMemberDto> PromoteToLeaderAsync(Guid groupId, Guid userId);

    Task<bool> LeaveGroupAsync(Guid groupId);
}
