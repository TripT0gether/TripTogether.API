using TripTogether.Application.DTOs.PollDTO;

namespace TripTogether.Application.Interfaces;

public interface IPollService
{
    Task<PollDto> CreatePollAsync(CreatePollDto dto);
    Task<PollDto> UpdatePollAsync(Guid pollId, UpdatePollDto dto);
    Task<bool> DeletePollAsync(Guid pollId);
    Task<PollDetailDto> GetPollDetailAsync(Guid pollId);
    Task<List<PollDto>> GetTripPollsAsync(Guid tripId);
    Task<PollDto> ClosePollAsync(Guid pollId);
    Task<PollOptionDto> AddPollOptionAsync(Guid pollId, CreatePollOptionDto dto);
    Task<bool> RemovePollOptionAsync(Guid optionId);
}
