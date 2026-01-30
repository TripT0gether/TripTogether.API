using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.TripInviteDTO;
using TripTogether.Application.Interfaces;

namespace TripTogether.API.Controllers;

[Route("api/trip-invites")]
[ApiController]
[Authorize]
public class TripInviteController : ControllerBase
{
    private readonly ITripInviteService _tripInviteService;

    public TripInviteController(ITripInviteService tripInviteService)
    {
        _tripInviteService = tripInviteService;
    }

    /// <summary>
    /// Create a new trip invite token.
    /// </summary>
    /// <param name="dto">Trip invite creation data.</param>
    /// <returns>Created trip invite with token.</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create trip invite",
        Description = "Create a new invite token for a trip. Only active group members can create invites."
    )]
    [ProducesResponseType(typeof(ApiResult<TripInviteDto>), 201)]
    [ProducesResponseType(typeof(ApiResult<TripInviteDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<TripInviteDto>), 404)]
    public async Task<IActionResult> CreateInvite([FromBody] CreateTripInviteDto dto)
    {
        var result = await _tripInviteService.CreateInviteAsync(dto);
        return StatusCode(201, ApiResult<TripInviteDto>.Success(result, "201", "Trip invite created successfully."));
    }

    /// <summary>
    /// Validate a trip invite token.
    /// </summary>
    /// <param name="token">The invite token to validate.</param>
    /// <returns>Validation result.</returns>
    [HttpGet("validate")]
    [SwaggerOperation(
        Summary = "Validate invite token",
        Description = "Check if a trip invite token is valid and not expired."
    )]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    public async Task<IActionResult> ValidateInviteToken([FromQuery] string token)
    {
        var result = await _tripInviteService.ValidateInviteTokenAsync(token);
        return Ok(ApiResult<bool>.Success(result, "200", result ? "Token is valid." : "Token is invalid or expired."));
    }

    /// <summary>
    /// Refresh a trip invite expiration.
    /// </summary>
    /// <param name="inviteId">Invite ID to refresh.</param>
    /// <returns>Updated trip invite.</returns>
    [HttpPatch("{inviteId:guid}/refresh")]
    [SwaggerOperation(
        Summary = "Refresh trip invite",
        Description = "Refresh an existing invite by extending its expiration time by 24 hours. Only active group members can refresh invites."
    )]
    [ProducesResponseType(typeof(ApiResult<TripInviteDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<TripInviteDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<TripInviteDto>), 404)]
    public async Task<IActionResult> RefreshInvite([FromRoute] Guid inviteId)
    {
        var result = await _tripInviteService.RefreshInviteAsync(inviteId);
        return Ok(ApiResult<TripInviteDto>.Success(result, "200", "Trip invite refreshed successfully."));
    }

    /// <summary>
    /// Revoke a trip invite.
    /// </summary>
    /// <param name="inviteId">Invite ID to revoke.</param>
    /// <returns>Revocation result.</returns>
    [HttpDelete("{inviteId:guid}")]
    [SwaggerOperation(
        Summary = "Revoke trip invite",
        Description = "Revoke/delete a trip invite. Only active group members can revoke invites."
    )]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<bool>), 403)]
    [ProducesResponseType(typeof(ApiResult<bool>), 404)]
    public async Task<IActionResult> RevokeInvite([FromRoute] Guid inviteId)
    {
        var result = await _tripInviteService.RevokeInviteAsync(inviteId);
        return Ok(ApiResult<bool>.Success(result, "200", "Trip invite revoked successfully."));
    }

    /// <summary>
    /// Get all invites for a specific trip.
    /// </summary>
    /// <param name="tripId">Trip ID.</param>
    /// <returns>List of trip invites.</returns>
    [HttpGet("trip/{tripId:guid}")]
    [SwaggerOperation(
        Summary = "Get trip invites",
        Description = "Get all invite tokens for a specific trip. Only active group members can view invites."
    )]
    [ProducesResponseType(typeof(ApiResult<List<TripInviteDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<List<TripInviteDto>>), 403)]
    [ProducesResponseType(typeof(ApiResult<List<TripInviteDto>>), 404)]
    public async Task<IActionResult> GetTripInvites([FromRoute] Guid tripId)
    {
        var result = await _tripInviteService.GetTripInvitesAsync(tripId);
        return Ok(ApiResult<List<TripInviteDto>>.Success(result, "200", "Trip invites retrieved successfully."));
    }
}
