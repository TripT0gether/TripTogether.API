using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.FriendshipDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Application.Utils;

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
    [ProducesResponseType(typeof(ApiResult<FriendshipDto>), 201)]
    [ProducesResponseType(typeof(ApiResult<FriendshipDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<FriendshipDto>), 404)]
    [ProducesResponseType(typeof(ApiResult<FriendshipDto>), 409)]
    public async Task<IActionResult> SendFriendRequest([FromBody] SendFriendRequestDto dto)
    {
        var result = await _friendshipService.SendFriendRequestAsync(dto);
        return StatusCode(201, ApiResult<FriendshipDto>.Success(result, "201", "Friend request sent successfully."));
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
        var result = await _friendshipService.AcceptFriendRequestAsync(requesterId);
        return Ok(ApiResult<FriendshipDto>.Success(result, "200", "Friend request accepted successfully."));
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
        var result = await _friendshipService.RejectFriendRequestAsync(requesterId);
        return Ok(ApiResult<object>.Success(result, "200", "Friend request rejected successfully."));
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
        var result = await _friendshipService.UnfriendAsync(friendId);
        return Ok(ApiResult<object>.Success(result, "200", "Unfriended successfully."));
    }

    /// <summary>
    /// Get the list of friends for the current user.
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 10).</param>
    /// <param name="searchTerm">Search by username or email.</param>
    /// <param name="sortBy">Not used - sorting is always by createdAt.</param>
    /// <param name="ascending">Sort order by createdAt (true for ascending, false for descending).</param>
    /// <returns>Paginated list of friends.</returns>
    [HttpGet("my-friends")]
    [SwaggerOperation(
        Summary = "Get my friends list",
        Description = "Retrieve the list of all accepted friends for the current user with search and sort options. Sorting is always by creation date (friendsSince)."
    )]
    [ProducesResponseType(typeof(ApiResult<Pagination<FriendListDto>>), 200)]
    public async Task<IActionResult> GetMyFriends(
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool ascending = true)
    {
        var result = await _friendshipService.GetFriendsListAsync(pageNumber, pageSize, searchTerm, sortBy, ascending);
        return Ok(ApiResult<Pagination<FriendListDto>>.Success(result, "200", "Friends list retrieved successfully."));
    }

    /// <summary>
    /// Get pending friend requests received by the current user.
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 10).</param>
    /// <param name="searchTerm">Search by username or email.</param>
    /// <param name="sortBy">Not used - sorting is always by createdAt.</param>
    /// <param name="ascending">Sort order by createdAt (true for ascending, false for descending, default is descending).</param>
    /// <returns>Paginated list of pending friend requests.</returns>
    [HttpGet("pending-requests")]
    [SwaggerOperation(
        Summary = "Get pending friend requests",
        Description = "Retrieve all pending friend requests that you have received with search and sort options. Sorting is always by request date."
    )]
    [ProducesResponseType(typeof(ApiResult<Pagination<FriendshipDto>>), 200)]
    public async Task<IActionResult> GetPendingRequests(
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool ascending = true)
    {
        var result = await _friendshipService.GetPendingRequestsAsync(pageNumber, pageSize, searchTerm, sortBy, ascending);
        return Ok(ApiResult<Pagination<FriendshipDto>>.Success(result, "200", "Pending requests retrieved successfully."));
    }

    /// <summary>
    /// Get friend requests sent by the current user.
    /// </summary>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 10).</param>
    /// <param name="searchTerm">Search by username or email.</param>
    /// <param name="sortBy">Not used - sorting is always by createdAt.</param>
    /// <param name="ascending">Sort order by createdAt (true for ascending, false for descending, default is descending).</param>
    /// <returns>Paginated list of sent friend requests.</returns>
    [HttpGet("sent-requests")]
    [SwaggerOperation(
        Summary = "Get sent friend requests",
        Description = "Retrieve all friend requests that you have sent and are still pending with search and sort options. Sorting is always by request date."
    )]
    [ProducesResponseType(typeof(ApiResult<Pagination<FriendshipDto>>), 200)]
    public async Task<IActionResult> GetSentRequests(
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null,
        [FromQuery] string? sortBy = null,
        [FromQuery] bool ascending = true)
    {
        var result = await _friendshipService.GetSentRequestsAsync(pageNumber, pageSize, searchTerm, sortBy, ascending);
        return Ok(ApiResult<Pagination<FriendshipDto>>.Success(result, "200", "Sent requests retrieved successfully."));
    }
}