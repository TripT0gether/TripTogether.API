using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.ActivityDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Application.Utils;

namespace TripTogether.API.Controllers;

[Route("api/activities")]
[ApiController]
[Authorize]
public class ActivityController : ControllerBase
{
    private readonly IActivityService _activityService;
    private readonly IFileService _fileService;

    public ActivityController(IActivityService activityService, IFileService fileService)
    {
        _activityService = activityService;
        _fileService = fileService;
    }

    /// <summary>
    /// Create a new activity for a trip.
    /// </summary>
    /// <param name="dto">Activity creation data.</param>
    /// <returns>Created activity information.</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create new activity",
        Description = "Create a new activity for a trip. The creator must be an active member of the trip's group."
    )]
    [ProducesResponseType(typeof(ApiResult<ActivityDto>), 201)]
    [ProducesResponseType(typeof(ApiResult<ActivityDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<ActivityDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<ActivityDto>), 404)]
    public async Task<IActionResult> CreateActivity([FromBody] CreateActivityDto dto)
    {
        var result = await _activityService.CreateActivityAsync(dto);
        return CreatedAtAction(
            nameof(GetActivityById),
            new { activityId = result.Id },
            ApiResult<ActivityDto>.Success(result, "201", "Activity created successfully.")
        );
    }

    /// <summary>
    /// Update activity information.
    /// </summary>
    /// <param name="activityId">Activity ID to update.</param>
    /// <param name="dto">Updated activity data.</param>
    /// <returns>Updated activity information.</returns>
    [HttpPut("{activityId:guid}")]
    [SwaggerOperation(
        Summary = "Update activity",
        Description = "Update activity information. Only active group members can update."
    )]
    [ProducesResponseType(typeof(ApiResult<ActivityDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<ActivityDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<ActivityDto>), 404)]
    public async Task<IActionResult> UpdateActivity([FromRoute] Guid activityId, [FromBody] UpdateActivityDto dto)
    {
        var result = await _activityService.UpdateActivityAsync(activityId, dto);
        return Ok(ApiResult<ActivityDto>.Success(result, "200", "Activity updated successfully."));
    }

    /// <summary>
    /// Delete an activity.
    /// </summary>
    /// <param name="activityId">Activity ID to delete.</param>
    /// <returns>Deletion result.</returns>
    [HttpDelete("{activityId:guid}")]
    [SwaggerOperation(
        Summary = "Delete activity",
        Description = "Delete an activity. Only active group members can delete activities."
    )]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<bool>), 403)]
    [ProducesResponseType(typeof(ApiResult<bool>), 404)]
    public async Task<IActionResult> DeleteActivity([FromRoute] Guid activityId)
    {
        var result = await _activityService.DeleteActivityAsync(activityId);
        return Ok(ApiResult<bool>.Success(result, "200", "Activity deleted successfully."));
    }

    /// <summary>
    /// Get activity details by ID.
    /// </summary>
    /// <param name="activityId">Activity ID.</param>
    /// <returns>Activity details.</returns>
    [HttpGet("{activityId:guid}")]
    [SwaggerOperation(
        Summary = "Get activity by ID",
        Description = "Retrieve detailed information about a specific activity."
    )]
    [ProducesResponseType(typeof(ApiResult<ActivityDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<ActivityDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<ActivityDto>), 404)]
    public async Task<IActionResult> GetActivityById([FromRoute] Guid activityId)
    {
        var result = await _activityService.GetActivityByIdAsync(activityId);
        return Ok(ApiResult<ActivityDto>.Success(result, "200", "Activity retrieved successfully."));
    }

    /// <summary>
    /// Get all activities for a trip.
    /// </summary>
    /// <param name="tripId">Trip ID.</param>
    /// <returns>List of activities for the trip.</returns>
    [HttpGet("trip/{tripId:guid}")]
    [SwaggerOperation(
        Summary = "Get activities by trip ID",
        Description = "Retrieve all activities for a specific trip. Activities are ordered by schedule day and start time."
    )]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<ActivityDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<ActivityDto>>), 403)]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<ActivityDto>>), 404)]
    public async Task<IActionResult> GetActivitiesByTripId([FromRoute] Guid tripId)
    {
        var result = await _activityService.GetActivitiesByTripIdAsync(tripId);
        return Ok(ApiResult<IEnumerable<ActivityDto>>.Success(result, "200", "Activities retrieved successfully."));
    }

    /// <summary>
    /// Get paginated activities for the current user with filtering and search.
    /// </summary>
    /// <param name="query">Query parameters for pagination, search, and filtering.</param>
    /// <returns>Paginated list of activities.</returns>
    [HttpGet("my-activities")]
    [SwaggerOperation(
        Summary = "Get my activities",
        Description = @"Retrieve activities from trips where the user is a member. 
                       Supports pagination, search (title, location, notes), 
                       filtering (status, category, date range, trip), 
                       and sorting (date, title, created). 
                       Default sort is by date ascending."
    )]
    [ProducesResponseType(typeof(ApiResult<Pagination<ActivityDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<Pagination<ActivityDto>>), 403)]
    public async Task<IActionResult> GetMyActivities([FromQuery] ActivityQueryDto query)
    {
        var result = await _activityService.GetMyActivitiesAsync(query);
        return Ok(ApiResult<Pagination<ActivityDto>>.Success(result, "200", "Activities retrieved successfully."));
    }

    /// <summary>
    /// Get available schedule day indexes for a specific date.
    /// </summary>
    /// <param name="tripId">Trip ID.</param>
    /// <param name="date">Date to check (format: yyyy-MM-dd).</param>
    /// <returns>List of available schedule day indexes (1-10).</returns>
    [HttpGet("available-indexes")]
    [SwaggerOperation(
        Summary = "Get available schedule day indexes",
        Description = "Get a list of available ScheduleDayIndex values (1-10) for a specific trip and date. Maximum 10 activities per day."
    )]
    [ProducesResponseType(typeof(ApiResult<List<int>>), 200)]
    [ProducesResponseType(typeof(ApiResult<List<int>>), 403)]
    [ProducesResponseType(typeof(ApiResult<List<int>>), 404)]
    public async Task<IActionResult> GetAvailableScheduleDayIndexes([FromQuery] Guid tripId, [FromQuery] DateOnly date)
    {
        var result = await _activityService.GetAvailableScheduleDayIndexesAsync(tripId, date);
        return Ok(ApiResult<List<int>>.Success(result, "200", $"Found {result.Count} available schedule indexes."));
    }

    /// <summary>
    /// Upload an image for an activity.
    /// </summary>
    /// <param name="activityId">Activity ID.</param>
    /// <param name="file">Image file to upload.</param>
    /// <returns>URL of the uploaded image.</returns>
    [HttpPost("{activityId:guid}/image")]
    [SwaggerOperation(
        Summary = "Upload activity image",
        Description = "Upload an image for a specific activity. Only active group members can upload images."
    )]
    [ProducesResponseType(typeof(ApiResult<string>), 200)]
    [ProducesResponseType(typeof(ApiResult<string>), 400)]
    [ProducesResponseType(typeof(ApiResult<string>), 403)]
    [ProducesResponseType(typeof(ApiResult<string>), 404)]
    public async Task<IActionResult> UploadActivityImage([FromRoute] Guid activityId, IFormFile file)
    {
        var imageUrl = await _fileService.UploadActivityImageAsync(activityId, file);
        return Ok(ApiResult<string>.Success(imageUrl, "200", "Activity image uploaded successfully."));
    }
}
