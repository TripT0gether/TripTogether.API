using Microsoft.AspNetCore.Http;
using TripTogether.Application.DTOs.GroupDTO;

namespace TripTogether.Application.Interfaces;

public interface IGroupService
{
    Task<GroupDto> CreateGroupAsync(CreateGroupDto dto);

    Task<GroupDto> UpdateGroupAsync(Guid groupId, UpdateGroupDto dto);

    Task<string> UploadCoverPhotoAsync(Guid groupId, IFormFile file);

    Task<bool> DeleteGroupAsync(Guid groupId);

    Task<GroupDetailDto> GetGroupDetailAsync(Guid groupId);

    Task<List<GroupDto>> GetMyGroupsAsync();

    Task<GroupMemberDto> InviteMemberAsync(Guid groupId, InviteMemberDto dto);

    Task<GroupMemberDto> AcceptInvitationAsync(Guid groupId);

    Task<bool> RejectInvitationAsync(Guid groupId);

    Task<bool> RemoveMemberAsync(Guid groupId, Guid userId);

    Task<GroupMemberDto> PromoteToLeaderAsync(Guid groupId, Guid userId);

    Task<bool> LeaveGroupAsync(Guid groupId);
}