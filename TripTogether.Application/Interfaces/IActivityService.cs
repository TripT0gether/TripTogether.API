using TripTogether.Application.DTOs.ActivityDTO;

namespace TripTogether.Application.Interfaces;

public interface IActivityService
{
    Task<ActivityDto> CreateActivityAsync(CreateActivityDto dto);
    Task<ActivityDto> UpdateActivityAsync(Guid activityId, UpdateActivityDto dto);
    Task<bool> DeleteActivityAsync(Guid activityId);
    Task<ActivityDto> GetActivityByIdAsync(Guid activityId);
    Task<IEnumerable<ActivitiesByDateDto>> GetActivitiesByTripIdAsync(Guid tripId);
    Task<Pagination<ActivitiesByDateDto>> GetMyActivitiesAsync(ActivityQueryDto query);
}
