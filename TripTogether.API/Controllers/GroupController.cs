using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.GroupDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Application.Utils;

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
    [ProducesResponseType(typeof(ApiResult<GroupDto>), 201)]
    [ProducesResponseType(typeof(ApiResult<GroupDto>), 400)]
    public async Task<IActionResult> CreateGroup([FromBody] CreateGroupDto dto)
    {
        var result = await _groupService.CreateGroupAsync(dto);
        return CreatedAtAction(
            nameof(GetGroupDetail),
            new { groupId = result.Id },
            ApiResult<GroupDto>.Success(result, "201", "Group created successfully.")
        );
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
        var result = await _groupService.UpdateGroupAsync(groupId, dto);
        return Ok(ApiResult<GroupDto>.Success(result, "200", "Group updated successfully."));
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
        var result = await _groupService.DeleteGroupAsync(groupId);
        return Ok(ApiResult<object>.Success(result, "200", "Group deleted successfully."));
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
        var result = await _groupService.GetGroupDetailAsync(groupId);
        return Ok(ApiResult<GroupDetailDto>.Success(result, "200", "Group details retrieved successfully."));
    }

    /// <summary>
    /// Get all groups the current user belongs to.
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 10).</param>
    /// <param name="searchTerm">Search by group name.</param>
    /// <param name="sortBy">Sort by field (createdat, date, membercount, members). Not used - always sorted by createdat or membercount.</param>
    /// <param name="ascending">Sort order (true for ascending, false for descending).</param>
    /// <returns>Paginated list of groups.</returns>
    [HttpGet("my-groups")]
    [SwaggerOperation(
        Summary = "Get my groups",
        Description = "Get all groups that the current user is a member of with search and sort options. Supports sorting by creation date (default) or member count."
    )]
    [ProducesResponseType(typeof(ApiResult<Pagination<GroupDto>>), 200)]
    public async Task<IActionResult> GetMyGroups(
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool ascending = true)
    {
        var result = await _groupService.GetMyGroupsAsync(pageNumber, pageSize, searchTerm, sortBy, ascending);
        return Ok(ApiResult<Pagination<GroupDto>>.Success(result, "200", "Groups retrieved successfully."));
    }

    /// <summary>
    /// Join a group using a trip invite token.
    /// </summary>
    /// <param name="token">The invite token.</param>
    /// <returns>Joined group information.</returns>
    [HttpPost("join")]
    [SwaggerOperation(
        Summary = "Join group by token",
        Description = "Join a group using a trip invite token. The user will be added as a member to the group associated with the trip."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<GroupDto>), 404)]
    [ProducesResponseType(typeof(ApiResult<GroupDto>), 409)]
    public async Task<IActionResult> JoinGroupByToken([FromQuery] string token)
    {
        var result = await _groupService.JoinGroupByToken(token);
        return Ok(ApiResult<GroupDto>.Success(result, "200", "Joined group successfully."));
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
        var result = await _groupService.InviteMemberAsync(groupId, dto);
        return Ok(ApiResult<GroupMemberDto>.Success(result, "200", "Member invited successfully."));
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
        var result = await _groupService.AcceptInvitationAsync(groupId);
        return Ok(ApiResult<GroupMemberDto>.Success(result, "200", "Invitation accepted successfully."));
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
        var result = await _groupService.RejectInvitationAsync(groupId);
        return Ok(ApiResult<object>.Success(result, "200", "Invitation rejected successfully."));
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
        var result = await _groupService.RemoveMemberAsync(groupId, userId);
        return Ok(ApiResult<object>.Success(result, "200", "Member removed successfully."));
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
        var result = await _groupService.PromoteToLeaderAsync(groupId, userId);
        return Ok(ApiResult<GroupMemberDto>.Success(result, "200", "Member promoted to leader successfully."));
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
        var result = await _groupService.LeaveGroupAsync(groupId);
        return Ok(ApiResult<object>.Success(result, "200", "Left group successfully."));
    }
}