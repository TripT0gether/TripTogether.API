using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.VoteDTO;
using TripTogether.Application.Interfaces;

namespace TripTogether.API.Controllers;

[Route("api/votes")]
[ApiController]
[Authorize]
public class VoteController : ControllerBase
{
    private readonly IVoteService _voteService;

    public VoteController(IVoteService voteService)
    {
        _voteService = voteService;
    }

    /// <summary>
    /// Cast a vote on a poll option.
    /// </summary>
    /// <param name="dto">Vote data containing the poll option ID.</param>
    /// <returns>Created vote information.</returns>
    [HttpPost]
    [SwaggerOperation(
        Summary = "Cast vote",
        Description = "Cast a vote on a poll option. User must be an active group member and poll must be open."
    )]
    [ProducesResponseType(typeof(ApiResult<VoteDto>), 201)]
    [ProducesResponseType(typeof(ApiResult<VoteDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<VoteDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<VoteDto>), 404)]
    [ProducesResponseType(typeof(ApiResult<VoteDto>), 409)]
    public async Task<IActionResult> CastVote([FromBody] CastVoteDto dto)
    {
        var result = await _voteService.CastVoteAsync(dto);
        return StatusCode(201, ApiResult<VoteDto>.Success(result, "201", "Vote cast successfully."));
    }

    /// <summary>
    /// Remove a vote.
    /// </summary>
    /// <param name="voteId">Vote ID to remove.</param>
    /// <returns>Removal result.</returns>
    [HttpDelete("{voteId:guid}")]
    [SwaggerOperation(
        Summary = "Remove vote",
        Description = "Remove your vote from a poll. Only the vote owner can remove their vote."
    )]
    [ProducesResponseType(typeof(ApiResult<bool>), 200)]
    [ProducesResponseType(typeof(ApiResult<bool>), 400)]
    [ProducesResponseType(typeof(ApiResult<bool>), 403)]
    [ProducesResponseType(typeof(ApiResult<bool>), 404)]
    public async Task<IActionResult> RemoveVote([FromRoute] Guid voteId)
    {
        var result = await _voteService.RemoveVoteAsync(voteId);
        return Ok(ApiResult<bool>.Success(result, "200", "Vote removed successfully."));
    }

    /// <summary>
    /// Change your vote to a different option.
    /// </summary>
    /// <param name="pollId">Poll ID.</param>
    /// <param name="newOptionId">New poll option ID to vote for.</param>
    /// <returns>Updated vote information.</returns>
    [HttpPut("poll/{pollId:guid}")]
    [SwaggerOperation(
        Summary = "Change vote",
        Description = "Change your vote to a different option in the same poll."
    )]
    [ProducesResponseType(typeof(ApiResult<VoteDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<VoteDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<VoteDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<VoteDto>), 404)]
    public async Task<IActionResult> ChangeVote([FromRoute] Guid pollId, [FromQuery] Guid newOptionId)
    {
        var result = await _voteService.ChangeVoteAsync(pollId, newOptionId);
        return Ok(ApiResult<VoteDto>.Success(result, "200", "Vote changed successfully."));
    }

    /// <summary>
    /// Get all votes for a specific poll.
    /// </summary>
    /// <param name="pollId">Poll ID.</param>
    /// <returns>List of votes for the poll.</returns>
    [HttpGet("poll/{pollId:guid}")]
    [SwaggerOperation(
        Summary = "Get poll votes",
        Description = "Get all votes for a specific poll. Only active group members can view votes."
    )]
    [ProducesResponseType(typeof(ApiResult<List<VoteDto>>), 200)]
    [ProducesResponseType(typeof(ApiResult<List<VoteDto>>), 403)]
    [ProducesResponseType(typeof(ApiResult<List<VoteDto>>), 404)]
    public async Task<IActionResult> GetPollVotes([FromRoute] Guid pollId)
    {
        var result = await _voteService.GetPollVotesAsync(pollId);
        return Ok(ApiResult<List<VoteDto>>.Success(result, "200", "Poll votes retrieved successfully."));
    }

    /// <summary>
    /// Get your vote for a specific poll.
    /// </summary>
    /// <param name="pollId">Poll ID.</param>
    /// <returns>Your vote for the poll, or null if you haven't voted.</returns>
    [HttpGet("poll/{pollId:guid}/my-vote")]
    [SwaggerOperation(
        Summary = "Get my vote",
        Description = "Get your vote for a specific poll. Returns null if you haven't voted yet."
    )]
    [ProducesResponseType(typeof(ApiResult<VoteDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<VoteDto>), 403)]
    [ProducesResponseType(typeof(ApiResult<VoteDto>), 404)]
    public async Task<IActionResult> GetUserVoteForPoll([FromRoute] Guid pollId)
    {
        var result = await _voteService.GetUserVoteForPollAsync(pollId);
        return Ok(ApiResult<VoteDto?>.Success(result, "200", result != null ? "Vote retrieved successfully." : "No vote found."));
    }
}
