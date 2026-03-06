using TripTogether.Application.DTOs.PollDTO;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Interfaces;

public interface IPollService
{
    Task<PollDto> CreatePollAsync(CreatePollDto dto);
    Task<PollDto> UpdatePollAsync(Guid pollId, UpdatePollDto dto);
    Task<bool> DeletePollAsync(Guid pollId);
    Task<PollDetailDto> GetPollDetailAsync(Guid pollId);
    Task<Pagination<PollDto>> GetPollsAsync(Guid tripId, PollScope scope = PollScope.All, int pageNumber = 1, int pageSize = 10);
    Task<PollDto> ClosePollAsync(Guid pollId);
    Task<PollDto> FinalizePollAsync(FinalizePollDto dto);
    Task<PollOptionDto> AddPollOptionAsync(Guid pollId, CreatePollOptionDto dto);
    Task<bool> RemovePollOptionAsync(Guid optionId);
}
