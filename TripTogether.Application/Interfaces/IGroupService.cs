using Microsoft.AspNetCore.Http;
using TripTogether.Application.DTOs.GroupDTO;
using TripTogether.Application.Utils;

namespace TripTogether.Application.Interfaces;

public interface IGroupService
{
    Task<GroupDto> CreateGroupAsync(CreateGroupDto dto);

    Task<GroupDto> UpdateGroupAsync(Guid groupId, UpdateGroupDto dto);

    Task<string> UploadCoverPhotoAsync(Guid groupId, IFormFile file);

    Task<bool> DeleteGroupAsync(Guid groupId);

    Task<GroupDetailDto> GetGroupDetailAsync(Guid groupId);

    Task<Pagination<GroupDto>> GetMyGroupsAsync(
        int pageNumber = 1, 
        int pageSize = 10, 
        string? searchTerm = null, 
        string? sortBy = null, 
        bool ascending = true);

    Task<GroupDto> JoinGroupByToken(string token);
}