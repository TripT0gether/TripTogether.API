using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.GroupDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Application.Services;

namespace TripTogether.API.Controllers;

[Route("api/groups/{groupId:guid}/members")]
[ApiController]
[Authorize]
public class GroupMemberController : ControllerBase
{
    private readonly IGroupMemberService _groupMemberService;

    public GroupMemberController(IGroupMemberService groupMemberService)
    {
        _groupMemberService = groupMemberService;
    }

    /// <summary>
    /// Invite a friend to join the group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="dto">Invitation data containing the user ID to invite.</param>
    /// <returns>Invited member information.</returns>
    [HttpPost("invite")]
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
        var result = await _groupMemberService.InviteMemberAsync(groupId, dto);
        return Ok(ApiResult<GroupMemberDto>.Success(result, "200", "Member invited successfully."));
    }

    /// <summary>
    /// Accept a group invitation.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>Accepted member information.</returns>
    [HttpPost("accept-invitation")]
    [SwaggerOperation(
        Summary = "Accept group invitation",
        Description = "Accept a pending invitation to join a group."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupMemberDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<GroupMemberDto>), 404)]
    public async Task<IActionResult> AcceptInvitation([FromRoute] Guid groupId)
    {
        var result = await _groupMemberService.AcceptInvitationAsync(groupId);
        return Ok(ApiResult<GroupMemberDto>.Success(result, "200", "Invitation accepted successfully."));
    }

    /// <summary>
    /// Reject a group invitation.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>Rejection result.</returns>
    [HttpDelete("reject-invitation")]
    [SwaggerOperation(
        Summary = "Reject group invitation",
        Description = "Reject a pending invitation to join a group."
    )]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> RejectInvitation([FromRoute] Guid groupId)
    {
        var result = await _groupMemberService.RejectInvitationAsync(groupId);
        return Ok(ApiResult<object>.Success(result, "200", "Invitation rejected successfully."));
    }

    /// <summary>
    /// Remove a member from the group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="userId">User ID to remove.</param>
    /// <returns>Removal result.</returns>
    [HttpDelete("{userId:guid}")]
    [SwaggerOperation(
        Summary = "Remove member from group",
        Description = "Remove a member from the group. Only group leaders can remove members."
    )]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 403)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> RemoveMember([FromRoute] Guid groupId, [FromRoute] Guid userId)
    {
        var result = await _groupMemberService.RemoveMemberAsync(groupId, userId);
        return Ok(ApiResult<object>.Success(result, "200", "Member removed successfully."));
    }

    /// <summary>
    /// Promote a member to group leader.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="userId">User ID to promote.</param>
    /// <returns>Promoted member information.</returns>
    [HttpPut("{userId:guid}/promote")]
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
        var result = await _groupMemberService.PromoteToLeaderAsync(groupId, userId);
        return Ok(ApiResult<GroupMemberDto>.Success(result, "200", "Member promoted to leader successfully."));
    }

    /// <summary>
    /// Leave a group.
    /// </summary>
    /// <param name="groupId">Group ID to leave.</param>
    /// <returns>Leave result.</returns>
    [HttpDelete("leave")]
    [SwaggerOperation(
        Summary = "Leave group",
        Description = "Leave a group. Group leaders must transfer leadership before leaving if there are other members."
    )]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 403)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> LeaveGroup([FromRoute] Guid groupId)
    {
        var result = await _groupMemberService.LeaveGroupAsync(groupId);
        return Ok(ApiResult<object>.Success(result, "200", "Left group successfully."));
    }
}
