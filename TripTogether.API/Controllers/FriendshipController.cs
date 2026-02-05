using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.FriendshipDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Domain.Enums;

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
    /// Search for users by username or email to send friend requests.
    /// </summary>
    /// <param name="searchTerm">Search term for username or email.</param>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 10).</param>
    /// <returns>Paginated list of users matching the search criteria.</returns>
    [HttpGet("search-users")]
    [SwaggerOperation(
        Summary = "Search for users",
        Description = "Search for users by username or email. Excludes current user, existing friends, and pending/blocked friendships. Only returns verified users."
    )]
    [ProducesResponseType(typeof(ApiResult<Pagination<UserSearchResultDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<Pagination<UserSearchResultDto>>), 400)]
    public async Task<IActionResult> SearchUsers(
        [FromQuery] string searchTerm,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10)
    {
        var dto = new SearchUsersDto
        {
            SearchTerm = searchTerm,
            PageNumber = pageNumber,
            PageSize = pageSize
        };

        var result = await _friendshipService.SearchUsersAsync(dto);
        return Ok(ApiResult<Pagination<UserSearchResultDto>>.Success(result, "200", "Users found successfully."));
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
    /// Accept a pending friend request by friendship ID.
    /// </summary>
    /// <param name="friendshipId">ID of the friendship record to accept.</param>
    /// <returns>Updated friendship information.</returns>
    [HttpPost("accept/{friendshipId:guid}")]
    [SwaggerOperation(
        Summary = "Accept friend request",
        Description = "Accept a pending friend request by the friendship ID."
    )]
    [ProducesResponseType(typeof(ApiResult<FriendshipDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<FriendshipDto>), 404)]
    [ProducesResponseType(typeof(ApiResult<FriendshipDto>), 403)]
    public async Task<IActionResult> AcceptFriendRequest([FromRoute] Guid friendshipId)
    {
        var result = await _friendshipService.AcceptFriendRequestAsync(friendshipId);
        return Ok(ApiResult<FriendshipDto>.Success(result, "200", "Friend request accepted successfully."));
    }

    /// <summary>
    /// Reject a pending friend request by friendship ID.
    /// </summary>
    /// <param name="friendshipId">ID of the friendship record to reject.</param>
    /// <returns>Rejection result.</returns>
    [HttpDelete("reject/{friendshipId:guid}")]
    [SwaggerOperation(
        Summary = "Reject friend request",
        Description = "Reject a pending friend request by the friendship ID."
    )]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    [ProducesResponseType(typeof(ApiResult<object>), 403)]
    public async Task<IActionResult> RejectFriendRequest([FromRoute] Guid friendshipId)
    {
        var result = await _friendshipService.RejectFriendRequestAsync(friendshipId);
        return Ok(ApiResult<object>.Success(result, "200", "Friend request rejected successfully."));
    }

    /// <summary>
    /// Unfriend a user (remove friendship) by friendship ID.
    /// </summary>
    /// <param name="friendshipId">ID of the friendship record to remove.</param>
    /// <returns>Unfriend result.</returns>
    [HttpDelete("unfriend/{friendshipId:guid}")]
    [SwaggerOperation(
        Summary = "Unfriend user",
        Description = "Remove friendship by the friendship ID."
    )]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    [ProducesResponseType(typeof(ApiResult<object>), 403)]
    public async Task<IActionResult> Unfriend([FromRoute] Guid friendshipId)
    {
        var result = await _friendshipService.UnfriendAsync(friendshipId);
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
    /// Get friend requests (received or sent) for the current user.
    /// </summary>
    /// <param name="type">Request type: Received (default) or Sent.</param>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 10).</param>
    /// <param name="searchTerm">Search by username.</param>
    /// <returns>Paginated list of friend requests.</returns>
    [HttpGet("requests")]
    [SwaggerOperation(
        Summary = "Get friend requests",
        Description = "Retrieve friend requests (received or sent) with search and pagination. Results are sorted by request date (newest first)."
    )]
    [ProducesResponseType(typeof(ApiResult<Pagination<FriendRequestDto>>), 200)]
    public async Task<IActionResult> GetFriendRequests(
        [FromQuery] FriendRequestType type = FriendRequestType.Received,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? searchTerm = null)
    {
        var result = await _friendshipService.GetFriendRequestsAsync(type, pageNumber, pageSize, searchTerm);
        var message = type == FriendRequestType.Received
            ? "Pending requests retrieved successfully."
            : "Sent requests retrieved successfully.";
        return Ok(ApiResult<Pagination<FriendRequestDto>>.Success(result, "200", message));
    }
}