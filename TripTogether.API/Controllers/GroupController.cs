using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.GroupDTO;
using TripTogether.Application.Interfaces;

namespace TripTogether.API.Controllers;

[Route("api/groups")]
[ApiController]
[Authorize]
public class GroupController : ControllerBase
{
    private readonly IGroupService _groupService;

    public GroupController(IGroupService groupService)
    {
        _groupService = groupService;
    }

    /// <summary>
    /// Create a new group.
    /// </summary>
    /// <param name="dto">Group creation data.</param>
    /// <returns>Created group information.</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create new group",
        Description = "Create a new group. The creator automatically becomes the group leader."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<GroupDto>), 400)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
    {
        try
        {
            var result = await _groupService.CreateGroupAsync(dto);
            return Ok(ApiResult<GroupDto>.Success(result, "200", "Group created successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<GroupDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Update group information.
    /// </summary>
    /// <param name="groupId">Group ID to update.</param>
    /// <param name="dto">Updated group data.</param>
    /// <returns>Updated group information.</returns>
    [HttpPut("{groupId:guid}")]
    [SwaggerOperation(
        Summary = "Update group",
        Description = "Update group information. Only group leaders can update."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<GroupDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<GroupDto>), 404)]
    public async Task<IActionResult> UpdateGroup([FromRoute] Guid groupId, [FromBody] UpdateGroupDto dto)
    {
        try
        {
            var result = await _groupService.UpdateGroupAsync(groupId, dto);
            return Ok(ApiResult<GroupDto>.Success(result, "200", "Group updated successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<GroupDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Upload group cover photo.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="file">Image file to upload.</param>
    /// <returns>URL of the uploaded cover photo.</returns>
    //[HttpPost("{groupId:guid}/cover-photo")]
    //[Consumes("multipart/form-data")]
    //[SwaggerOperation(
    //    Summary = "Upload group cover photo",
    //    Description = "Upload a cover photo for the group. Only group leaders can upload."
    //)]
    //[ProducesResponseType(typeof(ApiResult<string>), 200)]
    //[ProducesResponseType(typeof(ApiResult<string>), 400)]
    //[ProducesResponseType(typeof(ApiResult<string>), 403)]
    //[ProducesResponseType(typeof(ApiResult<string>), 404)]
    //public async Task<IActionResult> UploadCoverPhoto([FromRoute] Guid groupId, [FromForm] IFormFile file)
    //{
    //    try
    //    {
    //        if (file == null || file.Length == 0)
    //        {
    //            return BadRequest(ApiResult<string>.Failure("400", "File is required."));
    //        }

    //        var result = await _groupService.UploadCoverPhotoAsync(groupId, file);
    //        return Ok(ApiResult<string>.Success(result, "200", "Cover photo uploaded successfully."));
    //    }
    //    catch (Exception ex)
    //    {
    //        var statusCode = ExceptionUtils.ExtractStatusCode(ex);
    //        var errorResponse = ExceptionUtils.CreateErrorResponse<string>(ex);
    //        return StatusCode(statusCode, errorResponse);
    //    }
    //}

    /// <summary>
    /// Delete a group.
    /// </summary>
    /// <param name="groupId">Group ID to delete.</param>
    /// <returns>Deletion result.</returns>
    [HttpDelete("{groupId:guid}")]
    [SwaggerOperation(
        Summary = "Delete group",
        Description = "Delete a group. Only group leaders can delete groups."
    )]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 403)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> DeleteGroup([FromRoute] Guid groupId)
    {
        try
        {
            var result = await _groupService.DeleteGroupAsync(groupId);
            return Ok(ApiResult<object>.Success(result, "200", "Group deleted successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Get detailed information about a group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>Detailed group information including members.</returns>
    [HttpGet("{groupId:guid}")]
    [SwaggerOperation(
        Summary = "Get group details",
        Description = "Get detailed information about a group including all members."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupDetailDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<GroupDetailDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<GroupDetailDto>), 404)]
    public async Task<IActionResult> GetGroupDetail([FromRoute] Guid groupId)
    {
        try
        {
            var result = await _groupService.GetGroupDetailAsync(groupId);
            return Ok(ApiResult<GroupDetailDto>.Success(result, "200", "Group details retrieved successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<GroupDetailDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Get all groups the current user belongs to.
    /// </summary>
    /// <returns>List of groups.</returns>
    [HttpGet("my-groups")]
    [SwaggerOperation(
        Summary = "Get my groups",
        Description = "Get all groups that the current user is a member of."
    )]
    [ProducesResponseType(typeof(ApiResult<List<GroupDto>>), 200)]
    public async Task<IActionResult> GetMyGroups()
    {
        try
        {
            var result = await _groupService.GetMyGroupsAsync();
            return Ok(ApiResult<List<GroupDto>>.Success(result, "200", "Groups retrieved successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<GroupDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Invite a friend to join the group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="dto">Invitation data containing the user ID to invite.</param>
    /// <returns>Invited member information.</returns>
    [HttpPost("{groupId:guid}/invite")]
    [SwaggerOperation(
        Summary = "Invite member to group",
        Description = "Invite a friend to join the group. Only group leaders can invite members."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupMemberDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<GroupMemberDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<GroupMemberDto>), 404)]
    [ProducesResponseType(typeof(ApiResult<GroupMemberDto>), 409)]
    public async Task<IActionResult> InviteMember([FromRoute] Guid groupId, [FromBody] InviteMemberDto dto)
    {
        try
        {
            var result = await _groupService.InviteMemberAsync(groupId, dto);
            return Ok(ApiResult<GroupMemberDto>.Success(result, "200", "Member invited successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<GroupMemberDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Accept a group invitation.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>Accepted member information.</returns>
    [HttpPost("{groupId:guid}/accept-invitation")]
    [SwaggerOperation(
        Summary = "Accept group invitation",
        Description = "Accept a pending invitation to join a group."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupMemberDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<GroupMemberDto>), 404)]
    public async Task<IActionResult> AcceptInvitation([FromRoute] Guid groupId)
    {
        try
        {
            var result = await _groupService.AcceptInvitationAsync(groupId);
            return Ok(ApiResult<GroupMemberDto>.Success(result, "200", "Invitation accepted successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<GroupMemberDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Reject a group invitation.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>Rejection result.</returns>
    [HttpDelete("{groupId:guid}/reject-invitation")]
    [SwaggerOperation(
        Summary = "Reject group invitation",
        Description = "Reject a pending invitation to join a group."
    )]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> RejectInvitation([FromRoute] Guid groupId)
    {
        try
        {
            var result = await _groupService.RejectInvitationAsync(groupId);
            return Ok(ApiResult<object>.Success(result, "200", "Invitation rejected successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Remove a member from the group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="userId">User ID to remove.</param>
    /// <returns>Removal result.</returns>
    [HttpDelete("{groupId:guid}/members/{userId:guid}")]
    [SwaggerOperation(
        Summary = "Remove member from group",
        Description = "Remove a member from the group. Only group leaders can remove members."
    )]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 403)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> RemoveMember([FromRoute] Guid groupId, [FromRoute] Guid userId)
    {
        try
        {
            var result = await _groupService.RemoveMemberAsync(groupId, userId);
            return Ok(ApiResult<object>.Success(result, "200", "Member removed successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Promote a member to group leader.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="userId">User ID to promote.</param>
    /// <returns>Promoted member information.</returns>
    [HttpPut("{groupId:guid}/members/{userId:guid}/promote")]
    [SwaggerOperation(
        Summary = "Promote member to leader",
        Description = "Promote a member to group leader. Only current group leaders can promote members."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupMemberDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<GroupMemberDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<GroupMemberDto>), 404)]
    [ProducesResponseType(typeof(ApiResult<GroupMemberDto>), 409)]
    public async Task<IActionResult> PromoteToLeader([FromRoute] Guid groupId, [FromRoute] Guid userId)
    {
        try
        {
            var result = await _groupService.PromoteToLeaderAsync(groupId, userId);
            return Ok(ApiResult<GroupMemberDto>.Success(result, "200", "Member promoted to leader successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<GroupMemberDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Leave a group.
    /// </summary>
    /// <param name="groupId">Group ID to leave.</param>
    /// <returns>Leave result.</returns>
    [HttpPost("{groupId:guid}/leave")]
    [SwaggerOperation(
        Summary = "Leave group",
        Description = "Leave a group. Group leaders must transfer leadership before leaving if there are other members."
    )]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 403)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> LeaveGroup([FromRoute] Guid groupId)
    {
        try
        {
            var result = await _groupService.LeaveGroupAsync(groupId);
            return Ok(ApiResult<object>.Success(result, "200", "Left group successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}