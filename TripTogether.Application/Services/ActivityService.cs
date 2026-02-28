using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NpgsqlTypes;
using TripTogether.Application.DTOs.ActivityDTO;
using TripTogether.Application.Helpers;
using TripTogether.Application.Interfaces;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Services;

public sealed class ActivityService : IActivityService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly ILogger<ActivityService> _loggerService;

    public ActivityService(
        IUnitOfWork unitOfWork,
        IClaimsService claimsService,
        ILogger<ActivityService> loggerService)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _loggerService = loggerService;
    }

    public async Task<ActivityDto> CreateActivityAsync(CreateActivityDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} creating activity: {Title} for trip {TripId}",
            currentUserId, dto.Title, dto.TripId);

        var trip = await _unitOfWork.Trips.GetByIdAsync(dto.TripId);
        if (trip == null)
        {
            throw ErrorHelper.NotFound("The trip does not exist.");
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == trip.GroupId
                && gm.UserId == currentUserId
                && gm.Status == GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to create activities.");
        }

        TimeSlotHelper.ValidateTimeLogic(dto.StartTime, dto.EndTime);

        // Auto-assign ScheduleDayIndex if date is provided
        int? scheduleDayIndex = null;
        if (dto.Date != default(DateOnly))
        {
            scheduleDayIndex = await CalculateScheduleDayIndexAsync(
                dto.TripId,
                dto.Date,
                dto.StartTime,
                null);
        }

        var activity = new Activity
        {
            TripId = dto.TripId,
            Title = dto.Title,
            Status = dto.Status,
            Category = dto.Category,
            Date = dto.Date,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            ScheduleDayIndex = scheduleDayIndex,
            LocationName = dto.LocationName,
            GeoCoordinates = dto.Latitude.HasValue && dto.Longitude.HasValue
                ? new NpgsqlPoint(dto.Latitude.Value, dto.Longitude.Value)
                : null,
            LinkUrl = dto.LinkUrl,
            Notes = dto.Notes
        };

        await _unitOfWork.Activities.AddAsync(activity);

        // Reorder all activities on this date
        if (dto.Date != default(DateOnly))
        {
            await ReorderActivitiesOnDateAsync(dto.TripId, dto.Date);
        }

        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Activity {ActivityId} created successfully", activity.Id);

        return MapToDto(activity);
    }

    public async Task<ActivityDto> UpdateActivityAsync(Guid activityId, UpdateActivityDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} updating activity {ActivityId}",
            currentUserId, activityId);

        var activity = await _unitOfWork.Activities.GetQueryable()
            .Include(a => a.Trip)
            .FirstOrDefaultAsync(a => a.Id == activityId && !a.IsDeleted);

        if (activity == null)
        {
            throw ErrorHelper.NotFound("The activity does not exist.");
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == activity.Trip.GroupId
                && gm.UserId == currentUserId
                && gm.Status == GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to update activities.");
        }

        var startTimeToValidate = dto.StartTime ?? activity.StartTime;
        var endTimeToValidate = dto.EndTime ?? activity.EndTime;

        TimeSlotHelper.ValidateTimeLogic(startTimeToValidate, endTimeToValidate);

        var oldDate = activity.Date;
        var timeChanged = (dto.StartTime.HasValue && dto.StartTime != activity.StartTime) ||
                         (dto.EndTime.HasValue && dto.EndTime != activity.EndTime);
        var dateChanged = dto.Date.HasValue && dto.Date != activity.Date;

        if (dto.Title != null) activity.Title = dto.Title;
        if (dto.Status.HasValue) activity.Status = dto.Status.Value;
        if (dto.Category.HasValue) activity.Category = dto.Category;
        if (dto.Date.HasValue) activity.Date = dto.Date;
        if (dto.StartTime.HasValue) activity.StartTime = dto.StartTime;
        if (dto.EndTime.HasValue) activity.EndTime = dto.EndTime;
        if (dto.LocationName != null) activity.LocationName = dto.LocationName;

        if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            activity.GeoCoordinates = new NpgsqlPoint(dto.Latitude.Value, dto.Longitude.Value);
        }

        if (dto.LinkUrl != null) activity.LinkUrl = dto.LinkUrl;
        if (dto.Notes != null) activity.Notes = dto.Notes;

        await _unitOfWork.Activities.Update(activity);

        // Reorder if date changed or time changed
        if (dateChanged)
        {
            // Reorder old date
            if (oldDate.HasValue)
            {
                await ReorderActivitiesOnDateAsync(activity.TripId, oldDate.Value);
            }
            // Reorder new date
            if (activity.Date.HasValue)
            {
                await ReorderActivitiesOnDateAsync(activity.TripId, activity.Date.Value);
            }
        }
        else if (timeChanged && activity.Date.HasValue)
        {
            // Reorder same date if time changed
            await ReorderActivitiesOnDateAsync(activity.TripId, activity.Date.Value);
        }

        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Activity {ActivityId} updated successfully", activityId);

        return MapToDto(activity);
    }

    public async Task<bool> DeleteActivityAsync(Guid activityId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} deleting activity {ActivityId}",
            currentUserId, activityId);

        var activity = await _unitOfWork.Activities.GetQueryable()
            .Include(a => a.Trip)
            .FirstOrDefaultAsync(a => a.Id == activityId && !a.IsDeleted);

        if (activity == null)
        {
            throw ErrorHelper.NotFound("The activity does not exist.");
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == activity.Trip.GroupId
                && gm.UserId == currentUserId
                && gm.Status == GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to delete activities.");
        }

        await _unitOfWork.Activities.SoftRemoveRangeById(new List<Guid> { activityId });
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Activity {ActivityId} deleted successfully", activityId);

        return true;
    }

    public async Task<ActivityDto> GetActivityByIdAsync(Guid activityId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        var activity = await _unitOfWork.Activities.GetQueryable()
            .Include(a => a.Trip)
            .FirstOrDefaultAsync(a => a.Id == activityId && !a.IsDeleted);

        if (activity == null)
        {
            throw ErrorHelper.NotFound("The activity does not exist.");
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == activity.Trip.GroupId
                && gm.UserId == currentUserId
                && gm.Status == GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to view activities.");
        }

        return MapToDto(activity);
    }

    public async Task<IEnumerable<ActivitiesByDateDto>> GetActivitiesByTripIdAsync(Guid tripId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} retrieving activities for trip {TripId}",
            currentUserId, tripId);

        var trip = await _unitOfWork.Trips.GetByIdAsync(tripId);
        if (trip == null)
        {
            throw ErrorHelper.NotFound("The trip does not exist.");
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == trip.GroupId
                && gm.UserId == currentUserId
                && gm.Status == GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to view activities.");
        }

        var activities = await _unitOfWork.Activities.GetQueryable()
            .Where(a => a.TripId == tripId && !a.IsDeleted)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.ScheduleDayIndex)
            .ToListAsync();

        var activitiesByDate = activities
            .GroupBy(a => a.Date)
            .Select(g => new ActivitiesByDateDto
            {
                Date = g.Key,
                Activities = g.Select(MapToDto).ToList(),
                TotalActivities = g.Count()
            })
            .OrderBy(g => g.Date.HasValue ? 0 : 1)
            .ThenBy(g => g.Date)
            .ToList();

        return activitiesByDate;
    }

    public async Task<Pagination<ActivitiesByDateDto>> GetMyActivitiesAsync(ActivityQueryDto query)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} retrieving activities with filters", currentUserId);

        // Get all trip IDs where user is an active member (optimized query)
        var userTripIds = await _unitOfWork.Trips.GetQueryable()
            .Where(t => t.Group.Members.Any(gm =>
                gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Active))
            .Select(t => t.Id)
            .ToListAsync();

        if (!userTripIds.Any())
        {
            return new Pagination<ActivitiesByDateDto>(
                new List<ActivitiesByDateDto>(),
                0,
                query.PageNumber,
                query.PageSize
            );
        }

        // Build query
        var activitiesQuery = _unitOfWork.Activities.GetQueryable()
            .Include(a => a.Trip)
            .Where(a => userTripIds.Contains(a.TripId) && !a.IsDeleted);

        // Apply search filter
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchLower = query.SearchTerm.ToLower();
            activitiesQuery = activitiesQuery.Where(a =>
                a.Title.ToLower().Contains(searchLower) ||
                (a.LocationName != null && a.LocationName.ToLower().Contains(searchLower))
            );
        }

        // Apply status filter
        if (query.Status.HasValue)
        {
            activitiesQuery = activitiesQuery.Where(a => a.Status == query.Status.Value);
        }

        // Apply category filter
        if (query.Category.HasValue)
        {
            activitiesQuery = activitiesQuery.Where(a => a.Category == query.Category.Value);
        }

        // Apply date range filter
        if (query.FromDate.HasValue)
        {
            activitiesQuery = activitiesQuery.Where(a => a.Date >= query.FromDate.Value);
        }

        if (query.ToDate.HasValue)
        {
            activitiesQuery = activitiesQuery.Where(a => a.Date <= query.ToDate.Value);
        }

        activitiesQuery = activitiesQuery
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime);

        // Get all activities for grouping
        var allActivities = await activitiesQuery.ToListAsync();

        // Group by date
        var activitiesByDate = allActivities
            .GroupBy(a => a.Date)
            .Select(g => new ActivitiesByDateDto
            {
                Date = g.Key,
                Activities = g.Select(MapToDto).ToList(),
                TotalActivities = g.Count()
            })
            .OrderBy(g => g.Date.HasValue ? 0 : 1)
            .ThenBy(g => g.Date)
            .ToList();

        // Apply pagination to grouped results
        var totalCount = activitiesByDate.Count;
        var paginatedGroups = activitiesByDate
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToList();

        return new Pagination<ActivitiesByDateDto>(
            paginatedGroups,
            totalCount,
            query.PageNumber,
            query.PageSize
        );
    }

    private async Task<int> CalculateScheduleDayIndexAsync(Guid tripId, DateOnly date, TimeOnly? startTime, Guid? excludeActivityId)
    {
        var existingActivities = await _unitOfWork.Activities.GetQueryable()
            .Where(a => a.TripId == tripId && a.Date == date && !a.IsDeleted)
            .Where(a => excludeActivityId == null || a.Id != excludeActivityId)
            .ToListAsync();

        // Check max 10 activities per day
        if (existingActivities.Count >= 10)
        {
            throw ErrorHelper.BadRequest("Maximum of 10 activities per day has been reached for this date.");
        }

        // If no start time, assign to the end (after all existing activities)
        if (!startTime.HasValue)
        {
            return existingActivities.Count + 1;
        }

        // Sort existing activities by time (activities with time first, then without)
        var sortedActivities = existingActivities
            .OrderBy(a => a.StartTime.HasValue ? 0 : 1)
            .ThenBy(a => a.StartTime)
            .ToList();

        // Find the position where the new activity should be inserted
        int position = 1;
        foreach (var existing in sortedActivities)
        {
            // If existing activity has no time, insert before it
            if (!existing.StartTime.HasValue)
            {
                break;
            }

            // If new activity should come before this one, insert here
            if (existing.StartTime.Value > startTime.Value)
            {
                break;
            }

            // Check for time conflicts
            if (existing.StartTime.HasValue && existing.EndTime.HasValue)
            {
                if (startTime.Value < existing.EndTime.Value && startTime.Value >= existing.StartTime.Value)
                {
                    throw ErrorHelper.BadRequest($"Activity time conflicts with existing activity '{existing.Title}' ({existing.StartTime:HH:mm} - {existing.EndTime:HH:mm}).");
                }
            }

            position++;
        }

        return position;
    }

    private async Task ReorderActivitiesOnDateAsync(Guid tripId, DateOnly date)
    {
        var activities = await _unitOfWork.Activities.GetQueryable()
            .Where(a => a.TripId == tripId && a.Date == date && !a.IsDeleted)
            .ToListAsync();

        if (!activities.Any())
        {
            return;
        }

        // Sort: activities with time first (by time), then activities without time
        var sortedActivities = activities
            .OrderBy(a => a.StartTime.HasValue ? 0 : 1)
            .ThenBy(a => a.StartTime)
            .ToList();

        // Reassign ScheduleDayIndex sequentially
        for (int i = 0; i < sortedActivities.Count; i++)
        {
            sortedActivities[i].ScheduleDayIndex = i + 1;
            await _unitOfWork.Activities.Update(sortedActivities[i]);
        }
    }

    private ActivityDto MapToDto(Activity activity)
    {
        return new ActivityDto
        {
            Id = activity.Id,
            TripId = activity.TripId,
            Status = activity.Status,
            Title = activity.Title,
            Category = activity.Category,
            Date = activity.Date,
            StartTime = activity.StartTime,
            EndTime = activity.EndTime,
            ScheduleDayIndex = activity.ScheduleDayIndex,
            LocationName = activity.LocationName,
            Latitude = activity.GeoCoordinates?.X,
            Longitude = activity.GeoCoordinates?.Y,
            LinkUrl = activity.LinkUrl,
            ImageUrl = activity.ImageUrl,
            Notes = activity.Notes,
            CreatedAt = activity.CreatedAt,
            UpdatedAt = activity.UpdatedAt
        };
    }
}
