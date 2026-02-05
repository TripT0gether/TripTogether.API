using TripTogether.Application.DTOs.ActivityDTO;

namespace TripTogether.Application.Interfaces;

public interface IActivityService
{
    Task<ActivityDto> CreateActivityAsync(CreateActivityDto dto);
    Task<ActivityDto> UpdateActivityAsync(Guid activityId, UpdateActivityDto dto);
    Task<bool> DeleteActivityAsync(Guid activityId);
    Task<ActivityDto> GetActivityByIdAsync(Guid activityId);
    Task<IEnumerable<ActivityDto>> GetActivitiesByTripIdAsync(Guid tripId);
    Task<Pagination<ActivityDto>> GetMyActivitiesAsync(ActivityQueryDto query);
    Task<List<int>> GetAvailableScheduleDayIndexesAsync(Guid tripId, DateOnly date);
}
