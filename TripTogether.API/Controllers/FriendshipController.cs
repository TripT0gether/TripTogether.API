using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.FriendshipDTO;
using TripTogether.Application.Interfaces;

namespace TripTogether.API.Controllers;

[Route("api/friendships")]
[ApiController]
[Authorize]
public class FriendshipController : ControllerBase
{
    private readonly IFriendshipService _friendshipService;

    public FriendshipController(IFriendshipService friendshipService)
    {
        _friendshipService = friendshipService;
    }

    /// <summary>
    /// Send a friend request to another user.
    /// </summary>
    /// <param name="dto">Friend request data containing the target user ID.</param>
    /// <returns>Friend request information.</returns>
    [HttpPost("send-request")]
    [SwaggerOperation(
        Summary = "Send friend request",
        Description = "Send a friend request to another user by their user ID."
    )]
    [ProducesResponseType(typeof(ApiResult<FriendshipDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<FriendshipDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<FriendshipDto>), 404)]
    [ProducesResponseType(typeof(ApiResult<FriendshipDto>), 409)]
    public async Task<IActionResult> SendFriendRequest([FromBody] SendFriendRequestDto dto)
    {
        try
        {
            var result = await _friendshipService.SendFriendRequestAsync(dto);
            return Ok(ApiResult<FriendshipDto>.Success(result, "200", "Friend request sent successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<FriendshipDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Accept a pending friend request.
    /// </summary>
    /// <param name="requesterId">ID of the user who sent the friend request.</param>
    /// <returns>Updated friendship information.</returns>
    [HttpPost("accept/{requesterId:guid}")]
    [SwaggerOperation(
        Summary = "Accept friend request",
        Description = "Accept a pending friend request from the specified user."
    )]
    [ProducesResponseType(typeof(ApiResult<FriendshipDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<FriendshipDto>), 404)]
    public async Task<IActionResult> AcceptFriendRequest([FromRoute] Guid requesterId)
    {
        try
        {
            var result = await _friendshipService.AcceptFriendRequestAsync(requesterId);
            return Ok(ApiResult<FriendshipDto>.Success(result, "200", "Friend request accepted successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<FriendshipDto>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Reject a pending friend request.
    /// </summary>
    /// <param name="requesterId">ID of the user who sent the friend request.</param>
    /// <returns>Rejection result.</returns>
    [HttpDelete("reject/{requesterId:guid}")]
    [SwaggerOperation(
        Summary = "Reject friend request",
        Description = "Reject a pending friend request from the specified user."
    )]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> RejectFriendRequest([FromRoute] Guid requesterId)
    {
        try
        {
            var result = await _friendshipService.RejectFriendRequestAsync(requesterId);
            return Ok(ApiResult<object>.Success(result, "200", "Friend request rejected successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Unfriend a user (remove friendship).
    /// </summary>
    /// <param name="friendId">ID of the friend to remove.</param>
    /// <returns>Unfriend result.</returns>
    [HttpDelete("unfriend/{friendId:guid}")]
    [SwaggerOperation(
        Summary = "Unfriend user",
        Description = "Remove friendship with the specified user."
    )]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> Unfriend([FromRoute] Guid friendId)
    {
        try
        {
            var result = await _friendshipService.UnfriendAsync(friendId);
            return Ok(ApiResult<object>.Success(result, "200", "Unfriended successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<object>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Get the list of friends for the current user.
    /// </summary>
    /// <returns>List of friends.</returns>
    [HttpGet("my-friends")]
    [SwaggerOperation(
        Summary = "Get my friends list",
        Description = "Retrieve the list of all accepted friends for the current user."
    )]
    [ProducesResponseType(typeof(ApiResult<List<FriendListDto>>), 200)]
    public async Task<IActionResult> GetMyFriends()
    {
        try
        {
            var result = await _friendshipService.GetFriendsListAsync();
            return Ok(ApiResult<List<FriendListDto>>.Success(result, "200", "Friends list retrieved successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<FriendListDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Get pending friend requests received by the current user.
    /// </summary>
    /// <returns>List of pending friend requests.</returns>
    [HttpGet("pending-requests")]
    [SwaggerOperation(
        Summary = "Get pending friend requests",
        Description = "Retrieve all pending friend requests that you have received."
    )]
    [ProducesResponseType(typeof(ApiResult<List<FriendshipDto>>), 200)]
    public async Task<IActionResult> GetPendingRequests()
    {
        try
        {
            var result = await _friendshipService.GetPendingRequestsAsync();
            return Ok(ApiResult<List<FriendshipDto>>.Success(result, "200", "Pending requests retrieved successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<FriendshipDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }

    /// <summary>
    /// Get friend requests sent by the current user.
    /// </summary>
    /// <returns>List of sent friend requests.</returns>
    [HttpGet("sent-requests")]
    [SwaggerOperation(
        Summary = "Get sent friend requests",
        Description = "Retrieve all friend requests that you have sent and are still pending."
    )]
    [ProducesResponseType(typeof(ApiResult<List<FriendshipDto>>), 200)]
    public async Task<IActionResult> GetSentRequests()
    {
        try
        {
            var result = await _friendshipService.GetSentRequestsAsync();
            return Ok(ApiResult<List<FriendshipDto>>.Success(result, "200", "Sent requests retrieved successfully."));
        }
        catch (Exception ex)
        {
            var statusCode = ExceptionUtils.ExtractStatusCode(ex);
            var errorResponse = ExceptionUtils.CreateErrorResponse<List<FriendshipDto>>(ex);
            return StatusCode(statusCode, errorResponse);
        }
    }
}