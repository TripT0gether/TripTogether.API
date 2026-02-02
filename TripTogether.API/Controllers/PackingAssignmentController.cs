using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.PackingAssignmentDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Application.Utils;

namespace TripTogether.API.Controllers;

[Route("api/packing-assignments")]
[ApiController]
[Authorize]
public class PackingAssignmentController : ControllerBase
{
    private readonly IPackingAssignmentService _packingAssignmentService;

    public PackingAssignmentController(IPackingAssignmentService packingAssignmentService)
    {
        _packingAssignmentService = packingAssignmentService;
    }

    /// <summary>
    /// Create a new packing assignment.
    /// </summary>
    /// <param name="dto">Packing assignment creation data.</param>
    /// <returns>Created packing assignment information.</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create new packing assignment",
        Description = @"Assign a packing item to a user. The creator must be an active member of the trip's group.
                       If UserId is not provided (null), the item will be assigned to the current user (yourself)."
    )]
    [ProducesResponseType(typeof(ApiResult<PackingAssignmentDto>), 201)]
    [ProducesResponseType(typeof(ApiResult<PackingAssignmentDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<PackingAssignmentDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<PackingAssignmentDto>), 404)]
    public async Task<IActionResult> CreateAssignment([FromBody] CreatePackingAssignmentDto dto)
    {
        var result = await _packingAssignmentService.CreateAssignmentAsync(dto);
        return CreatedAtAction(
            nameof(GetAssignmentById),
            new { assignmentId = result.Id },
            ApiResult<PackingAssignmentDto>.Success(result, "201", "Packing assignment created successfully.")
        );
    }

    /// <summary>
    /// Update packing assignment information.
    /// </summary>
    /// <param name="assignmentId">Assignment ID to update.</param>
    /// <param name="dto">Updated assignment data.</param>
    /// <returns>Updated assignment information.</returns>
    [HttpPut("{assignmentId:guid}")]
    [SwaggerOperation(
        Summary = "Update packing assignment",
        Description = "Update assignment information (quantity, checked status). Only active group members can update."
    )]
    [ProducesResponseType(typeof(ApiResult<PackingAssignmentDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<PackingAssignmentDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<PackingAssignmentDto>), 404)]
    public async Task<IActionResult> UpdateAssignment([FromRoute] Guid assignmentId, [FromBody] UpdatePackingAssignmentDto dto)
    {
        var result = await _packingAssignmentService.UpdateAssignmentAsync(assignmentId, dto);
        return Ok(ApiResult<PackingAssignmentDto>.Success(result, "200", "Packing assignment updated successfully."));
    }

    /// <summary>
    /// Delete a packing assignment.
    /// </summary>
    /// <param name="assignmentId">Assignment ID to delete.</param>
    /// <returns>Deletion result.</returns>
    [HttpDelete("{assignmentId:guid}")]
    [SwaggerOperation(
        Summary = "Delete packing assignment",
        Description = "Delete a packing assignment. Only active group members can delete assignments."
    )]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<bool>), 403)]
    [ProducesResponseType(typeof(ApiResult<bool>), 404)]
    public async Task<IActionResult> DeleteAssignment([FromRoute] Guid assignmentId)
    {
        var result = await _packingAssignmentService.DeleteAssignmentAsync(assignmentId);
        return Ok(ApiResult<bool>.Success(result, "200", "Packing assignment deleted successfully."));
    }

    /// <summary>
    /// Get assignment details by ID.
    /// </summary>
    /// <param name="assignmentId">Assignment ID.</param>
    /// <returns>Assignment details.</returns>
    [HttpGet("{assignmentId:guid}")]
    [SwaggerOperation(
        Summary = "Get assignment by ID",
        Description = "Retrieve detailed information about a specific packing assignment."
    )]
    [ProducesResponseType(typeof(ApiResult<PackingAssignmentDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<PackingAssignmentDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<PackingAssignmentDto>), 404)]
    public async Task<IActionResult> GetAssignmentById([FromRoute] Guid assignmentId)
    {
        var result = await _packingAssignmentService.GetAssignmentByIdAsync(assignmentId);
        return Ok(ApiResult<PackingAssignmentDto>.Success(result, "200", "Packing assignment retrieved successfully."));
    }

    /// <summary>
    /// Get all assignments for a packing item.
    /// </summary>
    /// <param name="packingItemId">Packing item ID.</param>
    /// <returns>List of assignments for the packing item.</returns>
    [HttpGet("packing-item/{packingItemId:guid}")]
    [SwaggerOperation(
        Summary = "Get assignments by packing item ID",
        Description = "Retrieve all assignments for a specific packing item."
    )]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<PackingAssignmentDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<PackingAssignmentDto>>), 403)]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<PackingAssignmentDto>>), 404)]
    public async Task<IActionResult> GetAssignmentsByPackingItemId([FromRoute] Guid packingItemId)
    {
        var result = await _packingAssignmentService.GetAssignmentsByPackingItemIdAsync(packingItemId);
        return Ok(ApiResult<IEnumerable<PackingAssignmentDto>>.Success(result, "200", "Assignments retrieved successfully."));
    }

    /// <summary>
    /// Get assignment summary for a packing item.
    /// </summary>
    /// <param name="packingItemId">Packing item ID.</param>
    /// <returns>Summary showing total needed, assigned, and remaining quantities.</returns>
    [HttpGet("packing-item/{packingItemId:guid}/summary")]
    [SwaggerOperation(
        Summary = "Get packing item assignment summary",
        Description = @"Get a summary of assignments for a packing item including:
                       - Total quantity needed
                       - Total quantity assigned
                       - Remaining quantity to assign
                       - List of all assignments
                       Useful for shared items to track assignment progress."
    )]
    [ProducesResponseType(typeof(ApiResult<PackingItemAssignmentSummaryDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<PackingItemAssignmentSummaryDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<PackingItemAssignmentSummaryDto>), 404)]
    public async Task<IActionResult> GetAssignmentSummary([FromRoute] Guid packingItemId)
    {
        var result = await _packingAssignmentService.GetAssignmentSummaryAsync(packingItemId);
        return Ok(ApiResult<PackingItemAssignmentSummaryDto>.Success(result, "200", "Assignment summary retrieved successfully."));
    }

    /// <summary>
    /// Get all assignments for a user in a specific trip.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <param name="tripId">Trip ID.</param>
    /// <returns>List of assignments for the user in the trip.</returns>
    [HttpGet("user/{userId:guid}/trip/{tripId:guid}")]
    [SwaggerOperation(
        Summary = "Get assignments by user and trip",
        Description = "Retrieve all packing assignments for a specific user in a specific trip."
    )]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<PackingAssignmentDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<PackingAssignmentDto>>), 403)]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<PackingAssignmentDto>>), 404)]
    public async Task<IActionResult> GetAssignmentsByUserAndTrip([FromRoute] Guid userId, [FromRoute] Guid tripId)
    {
        var result = await _packingAssignmentService.GetAssignmentsByUserIdAsync(userId, tripId);
        return Ok(ApiResult<IEnumerable<PackingAssignmentDto>>.Success(result, "200", "User assignments retrieved successfully."));
    }
}
