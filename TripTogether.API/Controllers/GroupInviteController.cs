using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.GroupDTO;
using TripTogether.Application.DTOs.GroupInviteDTO;
using TripTogether.Application.Interfaces;

namespace TripTogether.API.Controllers;

[Route("api/group-invites")]
[ApiController]
[Authorize]
public sealed class GroupInviteController : ControllerBase
{
    private readonly IGroupInviteService _groupInviteService;

    public GroupInviteController(IGroupInviteService groupInviteService)
    {
        _groupInviteService = groupInviteService;
    }

    /// <summary>
    /// Create a new group invite token.
    /// </summary>
    /// <param name="dto">Group invite creation data.</param>
    /// <returns>Created group invite with token and invite URL.</returns>
    [HttpPost]
    [SwaggerOperation(
    Summary = "Create group invite",
        Description = "Create a new invite token for a group. Only active group members can create invites. Returns a shareable URL."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupInviteDto>), 201)]
    [ProducesResponseType(typeof(ApiResult<GroupInviteDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<GroupInviteDto>), 404)]
 [ProducesResponseType(typeof(ApiResult<GroupInviteDto>), 409)]
    public async Task<IActionResult> CreateInvite([FromBody] CreateGroupInviteDto dto)
    {
        var result = await _groupInviteService.CreateInviteAsync(dto);
        return StatusCode(201, ApiResult<GroupInviteDto>.Success(result, "201", "Group invite created successfully."));
    }

    /// <summary>
    /// Validate a group invite token.
    /// </summary>
    /// <param name="token">The invite token to validate.</param>
    /// <returns>Validation result.</returns>
    [HttpGet("validate")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Validate invite token",
        Description = "Check if a group invite token is valid and not expired. This endpoint is public."
    )]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    public async Task<IActionResult> ValidateInviteToken([FromQuery] string token)
    {
   var result = await _groupInviteService.ValidateInviteTokenAsync(token);
        return Ok(ApiResult<bool>.Success(result, "200", result ? "Token is valid." : "Token is invalid or expired."));
    }

    /// <summary>
    /// Get group info by invite token (preview before joining).
    /// </summary>
    /// <param name="token">The invite token.</param>
    /// <returns>Group invite information.</returns>
    [HttpGet("preview")]
    [AllowAnonymous]
    [SwaggerOperation(
        Summary = "Preview group by invite token",
 Description = "Get group information using an invite token. Useful for showing group preview before user joins."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupInviteDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<GroupInviteDto>), 404)]
  public async Task<IActionResult> GetInvitePreview([FromQuery] string token)
    {
  var result = await _groupInviteService.GetInviteByTokenAsync(token);
    return Ok(ApiResult<GroupInviteDto>.Success(result, "200", "Group invite retrieved successfully."));
    }

    /// <summary>
    /// Join a group using an invite token.
    /// </summary>
    /// <param name="token">The invite token.</param>
    /// <returns>Joined group information.</returns>
    [HttpPost("join")]
    [SwaggerOperation(
    Summary = "Join group by invite token",
        Description = "Join a group using a valid invite token. The user will be added as a regular member."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<GroupDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<GroupDto>), 404)]
    [ProducesResponseType(typeof(ApiResult<GroupDto>), 409)]
    public async Task<IActionResult> JoinGroupByToken([FromQuery] string token)
    {
        var result = await _groupInviteService.JoinGroupByTokenAsync(token);
 return Ok(ApiResult<GroupDto>.Success(result, "200", "Successfully joined the group."));
 }

    /// <summary>
    /// Refresh a group invite expiration.
    /// </summary>
    /// <param name="inviteId">Invite ID to refresh.</param>
    /// <returns>Updated group invite.</returns>
    [HttpPatch("{inviteId:guid}/refresh")]
    [SwaggerOperation(
        Summary = "Refresh group invite",
        Description = "Refresh an existing invite by extending its expiration time by 24 hours. Only active group members can refresh invites."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupInviteDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<GroupInviteDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<GroupInviteDto>), 404)]
    public async Task<IActionResult> RefreshInvite([FromRoute] Guid inviteId)
    {
        var result = await _groupInviteService.RefreshInviteAsync(inviteId);
        return Ok(ApiResult<GroupInviteDto>.Success(result, "200", "Group invite refreshed successfully."));
    }

    /// <summary>
    /// Revoke a group invite.
    /// </summary>
    /// <param name="inviteId">Invite ID to revoke.</param>
    /// <returns>Revocation result.</returns>
    [HttpDelete("{inviteId:guid}")]
    [SwaggerOperation(
        Summary = "Revoke group invite",
        Description = "Revoke/delete a group invite. Only active group members can revoke invites."
    )]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<bool>), 403)]
    [ProducesResponseType(typeof(ApiResult<bool>), 404)]
    public async Task<IActionResult> RevokeInvite([FromRoute] Guid inviteId)
    {
      var result = await _groupInviteService.RevokeInviteAsync(inviteId);
        return Ok(ApiResult<bool>.Success(result, "200", "Group invite revoked successfully."));
    }

    /// <summary>
    /// Get all invites for a specific group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>List of group invites.</returns>
    [HttpGet("group/{groupId:guid}")]
    [SwaggerOperation(
        Summary = "Get group invites",
        Description = "Get all invite tokens for a specific group. Only active group members can view invites."
    )]
    [ProducesResponseType(typeof(ApiResult<List<GroupInviteDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<List<GroupInviteDto>>), 403)]
    [ProducesResponseType(typeof(ApiResult<List<GroupInviteDto>>), 404)]
    public async Task<IActionResult> GetGroupInvites([FromRoute] Guid groupId)
    {
     var result = await _groupInviteService.GetGroupInvitesAsync(groupId);
   return Ok(ApiResult<List<GroupInviteDto>>.Success(result, "200", "Group invites retrieved successfully."));
    }

    /// <summary>
    /// Get active invite for a group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <returns>Active group invite or null.</returns>
    [HttpGet("group/{groupId:guid}/active")]
    [SwaggerOperation(
        Summary = "Get active group invite",
        Description = "Get the currently active (non-expired) invite for a group. Returns null if no active invite exists."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupInviteDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<GroupInviteDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<GroupInviteDto>), 404)]
    public async Task<IActionResult> GetActiveInvite([FromRoute] Guid groupId)
    {
   var result = await _groupInviteService.GetActiveInviteAsync(groupId);
        return Ok(ApiResult<GroupInviteDto?>.Success(result, "200", 
         result != null ? "Active invite retrieved successfully." : "No active invite found."));
    }
}
