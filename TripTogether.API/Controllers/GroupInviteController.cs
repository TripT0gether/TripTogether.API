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
public class GroupInviteController : ControllerBase
{
    private readonly IGroupInviteService _groupInviteService;

    public GroupInviteController(IGroupInviteService groupInviteService)
    {
        _groupInviteService = groupInviteService;
    }

    [HttpPost]
    [SwaggerOperation(
        Summary = "Create group invite",
        Description = "Create a new invite token for a group. Only active group members can create invites."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupInviteDto>), 201)]
    public async Task<IActionResult> CreateInvite([FromBody] CreateGroupInviteDto dto)
    {
        var result = await _groupInviteService.CreateInviteAsync(dto);
        return StatusCode(201, ApiResult<GroupInviteDto>.Success(result, "201", "Group invite created successfully."));
    }


    [HttpGet("token/{token}")]
    [SwaggerOperation(
        Summary = "Get invite by token",
        Description = "Get invite details using the token."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupInviteDto>), 200)]
    public async Task<IActionResult> GetInviteByToken([FromRoute] string token)
    {
        var result = await _groupInviteService.GetInviteByTokenAsync(token);
        return Ok(ApiResult<GroupInviteDto>.Success(result, "200", "Invite details retrieved successfully."));
    }

    [HttpPost("join")]
    [SwaggerOperation(
        Summary = "Join group by token",
        Description = "Join a group using an invite token."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupDto>), 200)]
    public async Task<IActionResult> JoinGroupByToken([FromQuery] string token)
    {
        var result = await _groupInviteService.JoinGroupByTokenAsync(token);
        return Ok(ApiResult<GroupDto>.Success(result, "200", "Successfully joined the group."));
    }

    [HttpPatch("{inviteId:guid}/refresh")]
    [SwaggerOperation(
        Summary = "Refresh group invite",
        Description = "Refresh an existing invite by extending its expiration time by 24 hours."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupInviteDto>), 200)]
    public async Task<IActionResult> RefreshInvite([FromRoute] Guid inviteId)
    {
        var result = await _groupInviteService.RefreshInviteAsync(inviteId);
        return Ok(ApiResult<GroupInviteDto>.Success(result, "200", "Group invite refreshed successfully."));
    }

    [HttpDelete("{inviteId:guid}")]
    [SwaggerOperation(
        Summary = "Revoke group invite",
        Description = "Revoke/delete a group invite."
    )]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    public async Task<IActionResult> RevokeInvite([FromRoute] Guid inviteId)
    {
        var result = await _groupInviteService.RevokeInviteAsync(inviteId);
        return Ok(ApiResult<bool>.Success(result, "200", "Group invite revoked successfully."));
    }


    [HttpGet("group/{groupId:guid}/active")]
    [SwaggerOperation(
        Summary = "Get active group invite",
        Description = "Get the currently active invite for a group."
    )]
    [ProducesResponseType(typeof(ApiResult<GroupInviteDto>), 200)]
    public async Task<IActionResult> GetActiveInvite([FromRoute] Guid groupId)
    {
        var result = await _groupInviteService.GetActiveInviteAsync(groupId);
        return Ok(ApiResult<GroupInviteDto?>.Success(result, "200", "Active invite retrieved successfully."));
    }
}
