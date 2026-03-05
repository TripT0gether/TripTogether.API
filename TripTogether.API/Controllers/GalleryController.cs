using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.GalleryDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Application.Utils;

namespace TripTogether.API.Controllers;

[Route("api/galleries")]
[ApiController]
[Authorize]
public class GalleryController : ControllerBase
{
    private readonly IGalleryService _galleryService;

    public GalleryController(IGalleryService galleryService)
    {
        _galleryService = galleryService;
    }

    /// <summary>
    /// Create a new gallery image.
    /// </summary>
    /// <param name="dto">Gallery creation data.</param>
    /// <returns>Created gallery image information.</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create new gallery image",
        Description = "Create a new gallery image for a trip or activity. The creator must be an active member of the group."
    )]
    [ProducesResponseType(typeof(ApiResult<GalleryDto>), 201)]
    [ProducesResponseType(typeof(ApiResult<GalleryDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<GalleryDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<GalleryDto>), 404)]
    public async Task<IActionResult> CreateGallery([FromBody] CreateGalleryDto dto)
    {
        var result = await _galleryService.CreateGalleryAsync(dto);
        return CreatedAtAction(
            nameof(GetGalleryById),
            new { galleryId = result.Id },
            ApiResult<GalleryDto>.Success(result, "201", "Gallery image created successfully.")
        );
    }

    /// <summary>
    /// Update gallery image information.
    /// </summary>
    /// <param name="galleryId">Gallery ID to update.</param>
    /// <param name="dto">Updated gallery data.</param>
    /// <returns>Updated gallery information.</returns>
    [HttpPut("{galleryId:guid}")]
    [SwaggerOperation(
        Summary = "Update gallery image",
        Description = "Update gallery image information. Only active group members can update."
    )]
    [ProducesResponseType(typeof(ApiResult<GalleryDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<GalleryDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<GalleryDto>), 404)]
    public async Task<IActionResult> UpdateGallery([FromRoute] Guid galleryId, [FromBody] UpdateGalleryDto dto)
    {
        var result = await _galleryService.UpdateGalleryAsync(galleryId, dto);
        return Ok(ApiResult<GalleryDto>.Success(result, "200", "Gallery image updated successfully."));
    }

    /// <summary>
    /// Delete a gallery image.
    /// </summary>
    /// <param name="galleryId">Gallery ID to delete.</param>
    /// <returns>Deletion result.</returns>
    [HttpDelete("{galleryId:guid}")]
    [SwaggerOperation(
        Summary = "Delete gallery image",
        Description = "Delete a gallery image. Only active group members can delete gallery images."
    )]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<bool>), 403)]
    [ProducesResponseType(typeof(ApiResult<bool>), 404)]
    public async Task<IActionResult> DeleteGallery([FromRoute] Guid galleryId)
    {
        var result = await _galleryService.DeleteGalleryAsync(galleryId);
        return Ok(ApiResult<bool>.Success(result, "200", "Gallery image deleted successfully."));
    }

    /// <summary>
    /// Get gallery image by ID.
    /// </summary>
    /// <param name="galleryId">Gallery ID.</param>
    /// <returns>Gallery image information.</returns>
    [HttpGet("{galleryId:guid}")]
    [SwaggerOperation(
        Summary = "Get gallery image by ID",
        Description = "Retrieve detailed information about a specific gallery image."
    )]
    [ProducesResponseType(typeof(ApiResult<GalleryDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<GalleryDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<GalleryDto>), 404)]
    public async Task<IActionResult> GetGalleryById([FromRoute] Guid galleryId)
    {
        var result = await _galleryService.GetGalleryByIdAsync(galleryId);
        return Ok(ApiResult<GalleryDto>.Success(result, "200", "Gallery image retrieved successfully."));
    }

    /// <summary>
    /// Get all gallery images with filtering and pagination.
    /// </summary>
    /// <param name="query">Query parameters for filtering and pagination.</param>
    /// <returns>List of gallery images.</returns>
    [HttpGet]
    [SwaggerOperation(
        Summary = "Get all galleries",
        Description = "Retrieve all gallery images with optional filtering by TripId, ActivityId, or search term. Only galleries from groups the user is a member of will be returned."
    )]
    [ProducesResponseType(typeof(ApiResult<IEnumerable<GalleryDto>>), 200)]
    public async Task<IActionResult> GetAllGalleries([FromQuery] GalleryQueryDto query)
    {
        var result = await _galleryService.GetAllGalleriesAsync(query);
        return Ok(ApiResult<IEnumerable<GalleryDto>>.Success(result, "200", "Gallery images retrieved successfully."));
    }
}
