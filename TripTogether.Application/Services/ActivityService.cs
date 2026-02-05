using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NpgsqlTypes;
using TripTogether.Application.DTOs.ActivityDTO;
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

        // Validate ScheduleDayIndex if provided
        if (dto.ScheduleDayIndex.HasValue)
        {
            if (dto.ScheduleDayIndex.Value < 1 || dto.ScheduleDayIndex.Value > 10)
            {
                throw ErrorHelper.BadRequest("ScheduleDayIndex must be between 1 and 10.");
            }

            // Date is required when ScheduleDayIndex is provided
            if (dto.Date == default(DateOnly))
            {
                throw ErrorHelper.BadRequest("Date is required when ScheduleDayIndex is specified.");
            }

            // Check if ScheduleDayIndex is already taken for this date
            var existingActivity = await _unitOfWork.Activities.GetQueryable()
                .AnyAsync(a => a.TripId == dto.TripId
                    && a.Date == dto.Date
                    && a.ScheduleDayIndex == dto.ScheduleDayIndex.Value);

            if (existingActivity)
            {
                throw ErrorHelper.BadRequest($"ScheduleDayIndex {dto.ScheduleDayIndex.Value} is already taken for this date. Please choose a different index (1-10).");
            }

            // Check if we've reached the maximum of 10 activities per day
            var activitiesOnDate = await _unitOfWork.Activities.GetQueryable()
                .CountAsync(a => a.TripId == dto.TripId && a.Date == dto.Date);

            if (activitiesOnDate >= 10)
            {
                throw ErrorHelper.BadRequest("Maximum of 10 activities per day has been reached for this date.");
            }
        }

        // Validate time logic (StartTime, EndTime, ScheduleSlot)
        ValidateTimeLogic(dto.StartTime, dto.EndTime, dto.ScheduleSlot);

        var activity = new Activity
        {
            TripId = dto.TripId,
            Title = dto.Title,
            Status = dto.Status,
            Category = dto.Category,
            Date = dto.Date,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            ScheduleDayIndex = dto.ScheduleDayIndex,
            ScheduleSlot = dto.ScheduleSlot,
            LocationName = dto.LocationName,
            GeoCoordinates = dto.Latitude.HasValue && dto.Longitude.HasValue
                ? new NpgsqlPoint(dto.Latitude.Value, dto.Longitude.Value)
                : null,
            LinkUrl = dto.LinkUrl,
            Notes = dto.Notes
        };

        await _unitOfWork.Activities.AddAsync(activity);
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
            .FirstOrDefaultAsync(a => a.Id == activityId);

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

        // Validate ScheduleDayIndex if being updated
        if (dto.ScheduleDayIndex.HasValue)
        {
            if (dto.ScheduleDayIndex.Value < 1 || dto.ScheduleDayIndex.Value > 10)
            {
                throw ErrorHelper.BadRequest("ScheduleDayIndex must be between 1 and 10.");
            }

            var dateToCheck = dto.Date ?? activity.Date;

            if (dateToCheck.HasValue)
            {
                // Check if ScheduleDayIndex is already taken by another activity on this date
                var existingActivity = await _unitOfWork.Activities.GetQueryable()
                    .AnyAsync(a => a.TripId == activity.TripId
                        && a.Id != activityId  // Exclude current activity
                        && a.Date == dateToCheck.Value
                        && a.ScheduleDayIndex == dto.ScheduleDayIndex.Value);

                if (existingActivity)
                {
                    throw ErrorHelper.BadRequest($"ScheduleDayIndex {dto.ScheduleDayIndex.Value} is already taken for this date. Please choose a different index (1-10).");
                }
            }
        }

        // If date is being changed, validate the new date doesn't exceed 10 activities
        if (dto.Date.HasValue && dto.Date.Value != activity.Date)
        {
            var activitiesOnNewDate = await _unitOfWork.Activities.GetQueryable()
                .CountAsync(a => a.TripId == activity.TripId
                    && a.Id != activityId  // Exclude current activity
                    && a.Date == dto.Date.Value);

            if (activitiesOnNewDate >= 10)
            {
                throw ErrorHelper.BadRequest("Maximum of 10 activities per day has been reached for the new date.");
            }
        }

        // Validate time logic if being updated
        var startTimeToValidate = dto.StartTime ?? activity.StartTime;
        var endTimeToValidate = dto.EndTime ?? activity.EndTime;
        var scheduleSlotToValidate = dto.ScheduleSlot ?? activity.ScheduleSlot;

        ValidateTimeLogic(startTimeToValidate, endTimeToValidate, scheduleSlotToValidate);

        if (dto.Title != null) activity.Title = dto.Title;
        if (dto.Status.HasValue) activity.Status = dto.Status.Value;
        if (dto.Category.HasValue) activity.Category = dto.Category;
        if (dto.Date.HasValue) activity.Date = dto.Date;
        if (dto.StartTime.HasValue) activity.StartTime = dto.StartTime;
        if (dto.EndTime.HasValue) activity.EndTime = dto.EndTime;
        if (dto.ScheduleDayIndex.HasValue) activity.ScheduleDayIndex = dto.ScheduleDayIndex;
        if (dto.ScheduleSlot.HasValue) activity.ScheduleSlot = dto.ScheduleSlot;
        if (dto.LocationName != null) activity.LocationName = dto.LocationName;

        if (dto.Latitude.HasValue && dto.Longitude.HasValue)
        {
            activity.GeoCoordinates = new NpgsqlPoint(dto.Latitude.Value, dto.Longitude.Value);
        }

        if (dto.LinkUrl != null) activity.LinkUrl = dto.LinkUrl;
        if (dto.Notes != null) activity.Notes = dto.Notes;

        await _unitOfWork.Activities.Update(activity);
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
            .FirstOrDefaultAsync(a => a.Id == activityId);

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
            .FirstOrDefaultAsync(a => a.Id == activityId);

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

    public async Task<IEnumerable<ActivityDto>> GetActivitiesByTripIdAsync(Guid tripId)
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
            .Where(a => a.TripId == tripId)
            .OrderBy(a => a.ScheduleDayIndex)
            .ThenBy(a => a.StartTime)
            .ToListAsync();

        return activities.Select(MapToDto);
    }

    public async Task<Pagination<ActivityDto>> GetMyActivitiesAsync(ActivityQueryDto query)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} retrieving activities with filters", currentUserId);

        // Get all trips where user is an active member
        var userTripIds = await _unitOfWork.GroupMembers.GetQueryable()
            .Where(gm => gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Active)
            .SelectMany(gm => gm.Group.Trips.Select(t => t.Id))
            .Distinct()
            .ToListAsync();

        if (!userTripIds.Any())
        {
            return new Pagination<ActivityDto>(
                new List<ActivityDto>(),
                0,
                query.PageNumber,
                query.PageSize
            );
        }

        // Build query
        var activitiesQuery = _unitOfWork.Activities.GetQueryable()
            .Include(a => a.Trip)
            .Where(a => userTripIds.Contains(a.TripId));

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

        activitiesQuery.OrderBy(a => a.Date).ThenBy(a => a.StartTime);

        // Get total count
        var totalCount = await activitiesQuery.CountAsync();

        // Apply pagination
        var activities = await activitiesQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var activityDtos = activities.Select(MapToDto).ToList();

        return new Pagination<ActivityDto>(
            activityDtos,
            totalCount,
            query.PageNumber,
            query.PageSize
        );
    }

    public async Task<List<int>> GetAvailableScheduleDayIndexesAsync(Guid tripId, DateOnly date)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} checking available schedule indexes for trip {TripId} on {Date}",
            currentUserId, tripId, date);

        // Verify user has access to this trip
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
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to view schedule indexes.");
        }

        // Get all used ScheduleDayIndexes for this date
        var usedIndexes = await _unitOfWork.Activities.GetQueryable()
            .Where(a => a.TripId == tripId && a.Date == date && a.ScheduleDayIndex.HasValue)
            .Select(a => a.ScheduleDayIndex.Value)
            .ToListAsync();

        // Return available indexes (1-10)
        var allIndexes = Enumerable.Range(1, 10).ToList();
        var availableIndexes = allIndexes.Except(usedIndexes).ToList();

        _loggerService.LogInformation("Found {Count} available schedule indexes for {Date}",
            availableIndexes.Count, date);

        return availableIndexes;
    }

    private void ValidateTimeLogic(TimeOnly? startTime, TimeOnly? endTime, TimeSlot? scheduleSlot)
    {
        // Validate StartTime and EndTime relationship
        if (startTime.HasValue && endTime.HasValue)
        {
            if (endTime.Value <= startTime.Value)
            {
                throw ErrorHelper.BadRequest("EndTime must be after StartTime.");
            }
        }

        // Validate ScheduleSlot matches time range
        if (scheduleSlot.HasValue && startTime.HasValue)
        {
            var expectedSlot = GetTimeSlotFromTime(startTime.Value);
            if (expectedSlot != scheduleSlot.Value)
            {
                var slotRange = GetTimeSlotRange(scheduleSlot.Value);
                throw ErrorHelper.BadRequest(
                    $"StartTime {startTime.Value:HH:mm} doesn't match ScheduleSlot {scheduleSlot.Value}. " +
                    $"Expected time range: {slotRange}");
            }
        }
    }

    private TimeSlot GetTimeSlotFromTime(TimeOnly time)
    {
        var hour = time.Hour;

        return hour switch
        {
            >= 6 and < 11 => TimeSlot.Morning,      // 06:00 - 10:59
            >= 11 and < 13 => TimeSlot.Lunch,       // 11:00 - 12:59
            >= 13 and < 17 => TimeSlot.Afternoon,   // 13:00 - 16:59
            >= 17 and < 19 => TimeSlot.Dinner,      // 17:00 - 18:59
            >= 19 and < 23 => TimeSlot.Evening,     // 19:00 - 22:59
            _ => TimeSlot.LateNight                 // 23:00 - 05:59
        };
    }

    private string GetTimeSlotRange(TimeSlot slot)
    {
        return slot switch
        {
            TimeSlot.Morning => "06:00 - 10:59",
            TimeSlot.Lunch => "11:00 - 12:59",
            TimeSlot.Afternoon => "13:00 - 16:59",
            TimeSlot.Dinner => "17:00 - 18:59",
            TimeSlot.Evening => "19:00 - 22:59",
            TimeSlot.LateNight => "23:00 - 05:59",
            _ => "Unknown"
        };
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
            ScheduleSlot = activity.ScheduleSlot,
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
