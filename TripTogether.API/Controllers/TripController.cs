using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.TripDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Application.Utils;
using TripTogether.Domain.Enums;

namespace TripTogether.API.Controllers;

[Route("api/trips")]
[ApiController]
[Authorize]
public class TripController : ControllerBase
{
    private readonly ITripService _tripService;

    public TripController(ITripService tripService)
    {
        _tripService = tripService;
    }

    /// <summary>
    /// Create a new trip for a group.
    /// </summary>
    /// <param name="dto">Trip creation data.</param>
    /// <returns>Created trip information with invite token.</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create new trip",
        Description = "Create a new trip for a group. The creator must be an active member of the group. An invite token is automatically generated."
    )]
    [ProducesResponseType(typeof(ApiResult<TripDto>), 201)]
    [ProducesResponseType(typeof(ApiResult<TripDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<TripDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<TripDto>), 404)]
    public async Task<IActionResult> CreateTrip([FromBody] CreateTripDto dto)
    {
        var result = await _tripService.CreateTripAsync(dto);
        return CreatedAtAction(
            nameof(GetTripDetail),
            new { tripId = result.Id },
            ApiResult<TripDto>.Success(result, "201", "Trip created successfully.")
        );
    }

    /// <summary>
    /// Update trip information.
    /// </summary>
    /// <param name="tripId">Trip ID to update.</param>
    /// <param name="dto">Updated trip data.</param>
    /// <returns>Updated trip information.</returns>
    [HttpPut("{tripId:guid}")]
    [SwaggerOperation(
        Summary = "Update trip",
        Description = "Update trip information. Only active group members can update."
    )]
    [ProducesResponseType(typeof(ApiResult<TripDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<TripDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<TripDto>), 404)]
    public async Task<IActionResult> UpdateTrip([FromRoute] Guid tripId, [FromBody] UpdateTripDto dto)
    {
        var result = await _tripService.UpdateTripAsync(tripId, dto);
        return Ok(ApiResult<TripDto>.Success(result, "200", "Trip updated successfully."));
    }

    /// <summary>
    /// Delete a trip.
    /// </summary>
    /// <param name="tripId">Trip ID to delete.</param>
    /// <returns>Deletion result.</returns>
    [HttpDelete("{tripId:guid}")]
    [SwaggerOperation(
        Summary = "Delete trip",
        Description = "Delete a trip. Only group leaders can delete trips."
    )]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<bool>), 403)]
    [ProducesResponseType(typeof(ApiResult<bool>), 404)]
    public async Task<IActionResult> DeleteTrip([FromRoute] Guid tripId)
    {
        var result = await _tripService.DeleteTripAsync(tripId);
        return Ok(ApiResult<bool>.Success(result, "200", "Trip deleted successfully."));
    }

    /// <summary>
    /// Get detailed information about a trip.
    /// </summary>
    /// <param name="tripId">Trip ID.</param>
    /// <returns>Detailed trip information including counts for polls, activities, and expenses.</returns>
    [HttpGet("{tripId:guid}")]
    [SwaggerOperation(
        Summary = "Get trip details",
        Description = "Get detailed information about a trip. Only active group members can view trip details."
    )]
    [ProducesResponseType(typeof(ApiResult<TripDetailDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<TripDetailDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<TripDetailDto>), 404)]
    public async Task<IActionResult> GetTripDetail([FromRoute] Guid tripId)
    {
        var result = await _tripService.GetTripDetailAsync(tripId);
        return Ok(ApiResult<TripDetailDto>.Success(result, "200", "Trip details retrieved successfully."));
    }

    /// <summary>
    /// Get all trips for a specific group.
    /// </summary>
    /// <param name="groupId">Group ID.</param>
    /// <param name="searchTerm">Search term to filter by title or group name.</param>
    /// <param name="status">Filter by trip status.</param>
    /// <param name="sortBy">Sort by field (CreatedAt, StartDate, PlanningRangeStart).</param>
    /// <param name="sortDescending">Sort in descending order (default: true).</param>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 10).</param>
    /// <returns>Paginated list of trips in the group.</returns>
    [HttpGet("group/{groupId:guid}")]
    [SwaggerOperation(
        Summary = "Get group trips",
        Description = "Get all trips for a specific group with optional search, filter, and sort capabilities. Only active group members can view the trips."
    )]
    [ProducesResponseType(typeof(ApiResult<Pagination<TripDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<Pagination<TripDto>>), 403)]
    [ProducesResponseType(typeof(ApiResult<Pagination<TripDto>>), 404)]
    public async Task<IActionResult> GetGroupTrips(
        [FromRoute] Guid groupId, 
        [FromQuery] string? searchTerm = null,
        [FromQuery] TripStatus? status = null,
        [FromQuery] TripSortBy sortBy = TripSortBy.CreatedAt,
        [FromQuery] bool sortDescending = true,
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10)
    {
        var query = new TripQueryDto
        {
            SearchTerm = searchTerm,
            Status = status,
            SortBy = sortBy,
            SortDescending = sortDescending,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await _tripService.GetGroupTripsAsync(groupId, query);
        return Ok(ApiResult<Pagination<TripDto>>.Success(result, "200", "Trips retrieved successfully."));
    }

    /// <summary>
    /// Update trip status.
    /// </summary>
    /// <param name="tripId">Trip ID.</param>
    /// <param name="status">New trip status.</param>
    /// <returns>Updated trip information.</returns>
    [HttpPatch("{tripId:guid}/status")]
    [SwaggerOperation(
        Summary = "Update trip status",
        Description = "Update the status of a trip. Only group leaders can update trip status."
    )]
    [ProducesResponseType(typeof(ApiResult<TripDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<TripDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<TripDto>), 404)]
    public async Task<IActionResult> UpdateTripStatus([FromRoute] Guid tripId, [FromQuery] TripStatus status)
    {
        var result = await _tripService.UpdateTripStatusAsync(tripId, status);
        return Ok(ApiResult<TripDto>.Success(result, "200", "Trip status updated successfully."));
    }

    /// <summary>
    /// Get all trips the current user has access to.
    /// </summary>
    /// <param name="searchTerm">Search term to filter by title or group name.</param>
    /// <param name="status">Filter by trip status.</param>
    /// <param name="sortBy">Sort by field (CreatedAt, StartDate, PlanningRangeStart).</param>
    /// <param name="sortDescending">Sort in descending order (default: true).</param>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 10).</param>
    /// <returns>Paginated list of trips from all groups the user is a member of.</returns>
    [HttpGet("my-trips")]
    [SwaggerOperation(
        Summary = "Get my trips",
        Description = "Get all trips from groups the current user is an active member of with optional search, filter, and sort capabilities."
    )]
    [ProducesResponseType(typeof(ApiResult<Pagination<TripDto>>), 200)]
    public async Task<IActionResult> GetMyTrips(
        [FromQuery] string? searchTerm = null,
        [FromQuery] TripStatus? status = null,
        [FromQuery] TripSortBy sortBy = TripSortBy.CreatedAt,
        [FromQuery] bool sortDescending = true,
        [FromQuery] int pageNumber = 1, 
        [FromQuery] int pageSize = 10)
    {
        var query = new TripQueryDto
        {
            SearchTerm = searchTerm,
            Status = status,
            SortBy = sortBy,
            SortDescending = sortDescending,
            PageNumber = pageNumber,
            PageSize = pageSize
        };
        var result = await _tripService.GetMyTripsAsync(query);
        return Ok(ApiResult<Pagination<TripDto>>.Success(result, "200", "My trips retrieved successfully."));
    }
}
