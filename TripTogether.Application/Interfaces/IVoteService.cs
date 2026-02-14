using TripTogether.Application.DTOs.VoteDTO;

namespace TripTogether.Application.Interfaces;

public interface IVoteService
{
    Task<VoteDto> CastVoteAsync(CastVoteDto dto);
    Task<bool> RemoveVoteAsync(Guid voteId);
    Task<VoteDto> ChangeVoteAsync(Guid pollId, Guid newOptionId);
    Task<List<VoteDto>> GetPollVotesAsync(Guid pollId);
    Task<List<VoteDto>> GetUserVotesForPollAsync(Guid pollId);
}
