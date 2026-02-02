using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.PackingItemDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Application.Utils;

namespace TripTogether.API.Controllers;

[Route("api/packing-items")]
[ApiController]
[Authorize]
public class PackingItemController : ControllerBase
{
    private readonly IPackingItemService _packingItemService;

    public PackingItemController(IPackingItemService packingItemService)
    {
        _packingItemService = packingItemService;
    }

    /// <summary>
    /// Create a new packing item for a trip.
    /// </summary>
    /// <param name="dto">Packing item creation data.</param>
    /// <returns>Created packing item information.</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create new packing item",
        Description = "Create a new packing item for a trip. The creator must be an active member of the trip's group."
    )]
    [ProducesResponseType(typeof(ApiResult<PackingItemDto>), 201)]
    [ProducesResponseType(typeof(ApiResult<PackingItemDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<PackingItemDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<PackingItemDto>), 404)]
    public async Task<IActionResult> CreatePackingItem([FromBody] CreatePackingItemDto dto)
    {
        var result = await _packingItemService.CreatePackingItemAsync(dto);
        return CreatedAtAction(
            nameof(GetPackingItemById),
            new { packingItemId = result.Id },
            ApiResult<PackingItemDto>.Success(result, "201", "Packing item created successfully.")
        );
    }

    /// <summary>
    /// Update packing item information.
    /// </summary>
    /// <param name="packingItemId">Packing item ID to update.</param>
    /// <param name="dto">Updated packing item data.</param>
    /// <returns>Updated packing item information.</returns>
    [HttpPut("{packingItemId:guid}")]
    [SwaggerOperation(
        Summary = "Update packing item",
        Description = "Update packing item information. Only active group members can update."
    )]
    [ProducesResponseType(typeof(ApiResult<PackingItemDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<PackingItemDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<PackingItemDto>), 404)]
    public async Task<IActionResult> UpdatePackingItem([FromRoute] Guid packingItemId, [FromBody] UpdatePackingItemDto dto)
    {
        var result = await _packingItemService.UpdatePackingItemAsync(packingItemId, dto);
        return Ok(ApiResult<PackingItemDto>.Success(result, "200", "Packing item updated successfully."));
    }

    /// <summary>
    /// Delete a packing item.
    /// </summary>
    /// <param name="packingItemId">Packing item ID to delete.</param>
    /// <returns>Deletion result.</returns>
    [HttpDelete("{packingItemId:guid}")]
    [SwaggerOperation(
        Summary = "Delete packing item",
        Description = "Delete a packing item. Only active group members can delete packing items."
    )]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<bool>), 403)]
    [ProducesResponseType(typeof(ApiResult<bool>), 404)]
    public async Task<IActionResult> DeletePackingItem([FromRoute] Guid packingItemId)
    {
        var result = await _packingItemService.DeletePackingItemAsync(packingItemId);
        return Ok(ApiResult<bool>.Success(result, "200", "Packing item deleted successfully."));
    }

    /// <summary>
    /// Get packing item details by ID.
    /// </summary>
    /// <param name="packingItemId">Packing item ID.</param>
    /// <returns>Packing item details.</returns>
    [HttpGet("{packingItemId:guid}")]
    [SwaggerOperation(
        Summary = "Get packing item by ID",
        Description = "Retrieve detailed information about a specific packing item."
    )]
    [ProducesResponseType(typeof(ApiResult<PackingItemDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<PackingItemDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<PackingItemDto>), 404)]
    public async Task<IActionResult> GetPackingItemById([FromRoute] Guid packingItemId)
    {
        var result = await _packingItemService.GetPackingItemByIdAsync(packingItemId);
        return Ok(ApiResult<PackingItemDto>.Success(result, "200", "Packing item retrieved successfully."));
    }

    /// <summary>
    /// Get all packing items for a trip.
    /// </summary>
    /// <param name="tripId">Trip ID.</param>
    /// <returns>List of packing items for the trip.</returns>
    [HttpGet("trip/{tripId:guid}")]
    [SwaggerOperation(
        Summary = "Get packing items by trip ID",
        Description = "Retrieve all packing items for a specific trip. Items are ordered by category and name."
    )]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<PackingItemDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<PackingItemDto>>), 403)]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<PackingItemDto>>), 404)]
    public async Task<IActionResult> GetPackingItemsByTripId([FromRoute] Guid tripId)
    {
        var result = await _packingItemService.GetPackingItemsByTripIdAsync(tripId);
        return Ok(ApiResult<IEnumerable<PackingItemDto>>.Success(result, "200", "Packing items retrieved successfully."));
    }
}
