using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.PollDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Application.Utils;

namespace TripTogether.API.Controllers;

[Route("api/polls")]
[ApiController]
[Authorize]
public class PollController : ControllerBase
{
    private readonly IPollService _pollService;

    public PollController(IPollService pollService)
    {
        _pollService = pollService;
    }

    /// <summary>
    /// Create a new poll for a trip.
    /// </summary>
    /// <param name="dto">Poll creation data including options.</param>
    /// <returns>Created poll information.</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Create new poll",
        Description = "Create a new poll for a trip with multiple options. The creator must be an active member of the group."
    )]
    [ProducesResponseType(typeof(ApiResult<PollDto>), 201)]
    [ProducesResponseType(typeof(ApiResult<PollDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<PollDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<PollDto>), 404)]
    public async Task<IActionResult> CreatePoll([FromBody] CreatePollDto dto)
    {
        var result = await _pollService.CreatePollAsync(dto);
        return CreatedAtAction(
            nameof(GetPollDetail),
            new { pollId = result.Id },
            ApiResult<PollDto>.Success(result, "201", "Poll created successfully.")
        );
    }

    /// <summary>
    /// Update poll information.
    /// </summary>
    /// <param name="pollId">Poll ID to update.</param>
    /// <param name="dto">Updated poll data.</param>
    /// <returns>Updated poll information.</returns>
    [HttpPut("{pollId:guid}")]
    [SwaggerOperation(
        Summary = "Update poll",
        Description = "Update poll title or status. Only active group members can update."
    )]
    [ProducesResponseType(typeof(ApiResult<PollDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<PollDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<PollDto>), 404)]
    public async Task<IActionResult> UpdatePoll([FromRoute] Guid pollId, [FromBody] UpdatePollDto dto)
    {
        var result = await _pollService.UpdatePollAsync(pollId, dto);
        return Ok(ApiResult<PollDto>.Success(result, "200", "Poll updated successfully."));
    }

    /// <summary>
    /// Delete a poll.
    /// </summary>
    /// <param name="pollId">Poll ID to delete.</param>
    /// <returns>Deletion result.</returns>
    [HttpDelete("{pollId:guid}")]
    [SwaggerOperation(
        Summary = "Delete poll",
        Description = "Delete a poll. Only the poll creator or group leaders can delete polls."
    )]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<bool>), 403)]
    [ProducesResponseType(typeof(ApiResult<bool>), 404)]
    public async Task<IActionResult> DeletePoll([FromRoute] Guid pollId)
    {
        var result = await _pollService.DeletePollAsync(pollId);
        return Ok(ApiResult<bool>.Success(result, "200", "Poll deleted successfully."));
    }

    /// <summary>
    /// Get detailed information about a poll.
    /// </summary>
    /// <param name="pollId">Poll ID.</param>
    /// <returns>Detailed poll information including all options and vote counts.</returns>
    [HttpGet("{pollId:guid}")]
    [SwaggerOperation(
        Summary = "Get poll details",
        Description = "Get detailed information about a poll including all options and votes. Only active group members can view."
    )]
    [ProducesResponseType(typeof(ApiResult<PollDetailDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<PollDetailDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<PollDetailDto>), 404)]
    public async Task<IActionResult> GetPollDetail([FromRoute] Guid pollId)
    {
        var result = await _pollService.GetPollDetailAsync(pollId);
        return Ok(ApiResult<PollDetailDto>.Success(result, "200", "Poll details retrieved successfully."));
    }

    /// <summary>
    /// Get all polls for a specific trip.
    /// </summary>
    /// <param name="tripId">Trip ID.</param>
    /// <param name="pageNumber">Page number (default: 1).</param>
    /// <param name="pageSize">Page size (default: 10).</param>
    /// <returns>Paginated list of polls in the trip.</returns>
    [HttpGet("trip/{tripId:guid}")]
    [SwaggerOperation(
        Summary = "Get trip polls",
        Description = "Get all polls for a specific trip. Only active group members can view the polls."
    )]
    [ProducesResponseType(typeof(ApiResult<Pagination<PollDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<Pagination<PollDto>>), 403)]
    [ProducesResponseType(typeof(ApiResult<Pagination<PollDto>>), 404)]
    public async Task<IActionResult> GetTripPolls([FromRoute] Guid tripId, [FromQuery] int pageNumber = 1, [FromQuery] int pageSize = 10)
    {
        var result = await _pollService.GetTripPollsAsync(tripId, pageNumber, pageSize);
        return Ok(ApiResult<Pagination<PollDto>>.Success(result, "200", "Trip polls retrieved successfully."));
    }

    /// <summary>
    /// Close a poll to prevent further voting.
    /// </summary>
    /// <param name="pollId">Poll ID to close.</param>
    /// <returns>Updated poll information.</returns>
    [HttpPatch("{pollId:guid}/close")]
    [SwaggerOperation(
        Summary = "Close poll",
        Description = "Close a poll to prevent further voting. Only the poll creator or group leaders can close polls."
    )]
    [ProducesResponseType(typeof(ApiResult<PollDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<PollDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<PollDto>), 404)]
    public async Task<IActionResult> ClosePoll([FromRoute] Guid pollId)
    {
        var result = await _pollService.ClosePollAsync(pollId);
        return Ok(ApiResult<PollDto>.Success(result, "200", "Poll closed successfully."));
    }

    /// <summary>
    /// Add a new option to an existing poll.
    /// </summary>
    /// <param name="pollId">Poll ID.</param>
    /// <param name="dto">Poll option data.</param>
    /// <returns>Created poll option information.</returns>
    [HttpPost("{pollId:guid}/options")]
    [SwaggerOperation(
        Summary = "Add poll option",
        Description = "Add a new option to an open poll. Only active group members can add options."
    )]
    [ProducesResponseType(typeof(ApiResult<PollOptionDto>), 201)]
    [ProducesResponseType(typeof(ApiResult<PollOptionDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<PollOptionDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<PollOptionDto>), 404)]
    public async Task<IActionResult> AddPollOption([FromRoute] Guid pollId, [FromBody] CreatePollOptionDto dto)
    {
        var result = await _pollService.AddPollOptionAsync(pollId, dto);
        return StatusCode(201, ApiResult<PollOptionDto>.Success(result, "201", "Poll option added successfully."));
    }

    /// <summary>
    /// Remove a poll option.
    /// </summary>
    /// <param name="optionId">Poll option ID to remove.</param>
    /// <returns>Removal result.</returns>
    [HttpDelete("options/{optionId:guid}")]
    [SwaggerOperation(
        Summary = "Remove poll option",
        Description = "Remove an option from an open poll. Only the option creator or group leaders can remove options."
    )]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<bool>), 400)]
    [ProducesResponseType(typeof(ApiResult<bool>), 403)]
    [ProducesResponseType(typeof(ApiResult<bool>), 404)]
    public async Task<IActionResult> RemovePollOption([FromRoute] Guid optionId)
    {
        var result = await _pollService.RemovePollOptionAsync(optionId);
        return Ok(ApiResult<bool>.Success(result, "200", "Poll option removed successfully."));
    }
}
