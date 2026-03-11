using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.AnnouncementDTO;
using TripTogether.Application.Interfaces;

namespace TripTogether.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class AnnouncementsController : ControllerBase
{
    private readonly IAnnouncementService _announcementService;

    public AnnouncementsController(IAnnouncementService announcementService)
    {
        _announcementService = announcementService;
    }

    /// <summary>
    /// Get paginated list of announcements for the current user.
    /// </summary>
    /// <param name="query">Query parameters for filtering and pagination.</param>
    /// <returns>Paginated list of announcements.</returns>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get my announcements",
        Description = "Retrieve a paginated list of announcements for the authenticated user with optional filters."
    )]
    [ProducesResponseType(typeof(ApiResult<Pagination<AnnouncementDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 401)]
    public async Task<IActionResult> GetMyAnnouncements([FromQuery] AnnouncementQueryDto query)
    {
        var result = await _announcementService.GetMyAnnouncementsAsync(query);
        return Ok(ApiResult<Pagination<AnnouncementDto>>.Success(result, "200", "Announcements retrieved successfully."));
    }

    /// <summary>
    /// Get a specific announcement by ID.
    /// </summary>
    /// <param name="announcementId">The unique identifier of the announcement.</param>
    /// <returns>Announcement details.</returns>
    [HttpGet("{announcementId:guid}")]
    [SwaggerOperation(
        Summary = "Get announcement by ID",
        Description = "Retrieve detailed information about a specific announcement."
    )]
    [ProducesResponseType(typeof(ApiResult<AnnouncementDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 401)]
    [ProducesResponseType(typeof(ApiResult<object>), 403)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> GetAnnouncementById(Guid announcementId)
    {
        var result = await _announcementService.GetAnnouncementByIdAsync(announcementId);
        return Ok(ApiResult<AnnouncementDto>.Success(result, "200", "Announcement retrieved successfully."));
    }

    /// <summary>
    /// Get the count of unread announcements for the current user.
    /// </summary>
    /// <returns>Number of unread announcements.</returns>
    [HttpGet("unread-count")]
    [SwaggerOperation(
        Summary = "Get unread count",
        Description = "Get the total number of unread announcements for the authenticated user."
    )]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 401)]
    public async Task<IActionResult> GetUnreadCount()
    {
        var count = await _announcementService.GetUnreadCountAsync();
        return Ok(ApiResult<object>.Success(new { count }, "200", "Unread count retrieved successfully."));
    }

    /// <summary>
    /// Mark a specific announcement as read.
    /// </summary>
    /// <param name="announcementId">The unique identifier of the announcement to mark as read.</param>
    /// <returns>Success result.</returns>
    [HttpPut("{announcementId:guid}/mark-read")]
    [SwaggerOperation(
        Summary = "Mark announcement as read",
        Description = "Mark a specific announcement as read for the current user."
    )]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 400)]
    [ProducesResponseType(typeof(ApiResult<object>), 401)]
    [ProducesResponseType(typeof(ApiResult<object>), 403)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> MarkAsRead(Guid announcementId)
    {
        await _announcementService.MarkAsReadAsync(announcementId);
        return Ok(ApiResult<object>.Success(null, "200", "Announcement marked as read."));
    }

    /// <summary>
    /// Mark all announcements as read for the current user.
    /// </summary>
    /// <returns>Success result.</returns>
    [HttpPut("mark-all-read")]
    [SwaggerOperation(
        Summary = "Mark all announcements as read",
        Description = "Mark all announcements as read for the authenticated user."
    )]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 401)]
    public async Task<IActionResult> MarkAllAsRead()
    {
        await _announcementService.MarkAllAsReadAsync();
        return Ok(ApiResult<object>.Success(null, "200", "All announcements marked as read."));
    }
}
