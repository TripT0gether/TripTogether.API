using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TripTogether.Application.DTOs.VoteDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Services;

public sealed class VoteService : IVoteService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly ILogger _loggerService;

    public VoteService(
        IUnitOfWork unitOfWork,
        IClaimsService claimsService,
        ILogger<VoteService> loggerService)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _loggerService = loggerService;
    }

    public async Task<VoteDto> CastVoteAsync(CastVoteDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} casting vote for option {dto.PollOptionId}");

        var pollOption = await _unitOfWork.PollOptions.GetQueryable()
            .Include(po => po.Poll)
            .ThenInclude(p => p.Trip)
            .ThenInclude(t => t.Group)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(po => po.Id == dto.PollOptionId);

        if (pollOption == null)
        {
            throw ErrorHelper.NotFound("The poll option does not exist.");
        }

        var isGroupMember = pollOption.Poll.Trip.Group.Members.Any(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to vote.");
        }

        if (pollOption.Poll.Status == PollStatus.Closed)
        {
            throw ErrorHelper.BadRequest("Cannot vote on a closed poll.");
        }

        // Check if user already voted for this poll
        var existingVote = await _unitOfWork.Votes.GetQueryable()
            .Include(v => v.PollOption)
            .FirstOrDefaultAsync(v => v.UserId == currentUserId && v.PollOption.PollId == pollOption.PollId);

        if (existingVote != null)
        {
            throw ErrorHelper.Conflict("You have already voted on this poll. Use change vote to update your vote.");
        }

        var vote = new Vote
        {
            PollOptionId = dto.PollOptionId,
            UserId = currentUserId,
            CreatedBy = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Votes.AddAsync(vote);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Vote {vote.Id} cast successfully by user {currentUserId}");

        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId);

        return new VoteDto
        {
            Id = vote.Id,
            PollOptionId = vote.PollOptionId,
            UserId = vote.UserId,
            Username = user?.Username ?? "Unknown",
            CreatedAt = vote.CreatedAt
        };
    }

    public async Task<bool> RemoveVoteAsync(Guid voteId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} removing vote {voteId}");

        var vote = await _unitOfWork.Votes.GetQueryable()
            .Include(v => v.PollOption)
            .ThenInclude(po => po.Poll)
            .FirstOrDefaultAsync(v => v.Id == voteId);

        if (vote == null)
        {
            throw ErrorHelper.NotFound("The vote does not exist.");
        }

        if (vote.UserId != currentUserId)
        {
            throw ErrorHelper.Forbidden("You can only remove your own votes.");
        }

        if (vote.PollOption.Poll.Status == PollStatus.Closed)
        {
            throw ErrorHelper.BadRequest("Cannot remove vote from a closed poll.");
        }

        await _unitOfWork.Votes.SoftRemove(vote);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Vote {voteId} removed successfully");

        return true;
    }

    public async Task<VoteDto> ChangeVoteAsync(Guid pollId, Guid newOptionId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} changing vote for poll {pollId} to option {newOptionId}");

        // Get the poll and verify permissions
        var poll = await _unitOfWork.Polls.GetQueryable()
            .Include(p => p.Trip)
            .ThenInclude(t => t.Group)
            .ThenInclude(g => g.Members)
            .Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.Id == pollId);

        if (poll == null)
        {
            throw ErrorHelper.NotFound("The poll does not exist.");
        }

        var isGroupMember = poll.Trip.Group.Members.Any(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to vote.");
        }

        if (poll.Status == PollStatus.Closed)
        {
            throw ErrorHelper.BadRequest("Cannot change vote on a closed poll.");
        }

        // Verify new option belongs to this poll
        var newOption = poll.Options.FirstOrDefault(o => o.Id == newOptionId);
        if (newOption == null)
        {
            throw ErrorHelper.BadRequest("The specified option does not belong to this poll.");
        }

        // Find existing vote
        var existingVote = await _unitOfWork.Votes.GetQueryable()
            .Include(v => v.PollOption)
            .FirstOrDefaultAsync(v => v.UserId == currentUserId && v.PollOption.PollId == pollId);

        if (existingVote == null)
        {
            throw ErrorHelper.NotFound("You have not voted on this poll yet. Use cast vote instead.");
        }

        // Remove old vote
        await _unitOfWork.Votes.SoftRemove(existingVote);

        // Create new vote
        var newVote = new Vote
        {
            PollOptionId = newOptionId,
            UserId = currentUserId,
            CreatedBy = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Votes.AddAsync(newVote);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Vote changed successfully from option {existingVote.PollOptionId} to {newOptionId}");

        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId);

        return new VoteDto
        {
            Id = newVote.Id,
            PollOptionId = newVote.PollOptionId,
            UserId = newVote.UserId,
            Username = user?.Username ?? "Unknown",
            CreatedAt = newVote.CreatedAt
        };
    }

    public async Task<List<VoteDto>> GetPollVotesAsync(Guid pollId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} getting votes for poll {pollId}");

        var poll = await _unitOfWork.Polls.GetQueryable()
            .Include(p => p.Trip)
            .ThenInclude(t => t.Group)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(p => p.Id == pollId);

        if (poll == null)
        {
            throw ErrorHelper.NotFound("The poll does not exist.");
        }

        var isGroupMember = poll.Trip.Group.Members.Any(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to view votes.");
        }

        var votes = await _unitOfWork.Votes.GetQueryable()
            .Include(v => v.PollOption)
            .Include(v => v.User)
            .Where(v => v.PollOption.PollId == pollId)
            .OrderBy(v => v.CreatedAt)
            .ToListAsync();

        return votes.Select(v => new VoteDto
        {
            Id = v.Id,
            PollOptionId = v.PollOptionId,
            UserId = v.UserId,
            Username = v.User.Username,
            CreatedAt = v.CreatedAt
        }).ToList();
    }

    public async Task<VoteDto?> GetUserVoteForPollAsync(Guid pollId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} getting their vote for poll {pollId}");

        var poll = await _unitOfWork.Polls.GetQueryable()
            .Include(p => p.Trip)
            .ThenInclude(t => t.Group)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(p => p.Id == pollId);

        if (poll == null)
        {
            throw ErrorHelper.NotFound("The poll does not exist.");
        }

        var isGroupMember = poll.Trip.Group.Members.Any(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to view votes.");
        }

        var vote = await _unitOfWork.Votes.GetQueryable()
            .Include(v => v.PollOption)
            .Include(v => v.User)
            .FirstOrDefaultAsync(v => v.UserId == currentUserId && v.PollOption.PollId == pollId);

        if (vote == null)
        {
            return null;
        }

        return new VoteDto
        {
            Id = vote.Id,
            PollOptionId = vote.PollOptionId,
            UserId = vote.UserId,
            Username = vote.User.Username,
            CreatedAt = vote.CreatedAt
        };
    }
}
