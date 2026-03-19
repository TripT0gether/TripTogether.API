using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TripTogether.Application.DTOs.ActivityDTO;
using TripTogether.Application.DTOs.PackingItemDTO;
using TripTogether.Application.DTOs.PollDTO;
using TripTogether.Application.DTOs.TripDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Services;

public sealed class TripService : ITripService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly ILogger _loggerService;
    private readonly IAnnouncementService _announcementService;

    public TripService(
        IUnitOfWork unitOfWork,
        IClaimsService claimsService,
        ILogger<TripService> loggerService,
        IAnnouncementService announcementService)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _loggerService = loggerService;
        _announcementService = announcementService;
    }

    public async Task<TripDto> CreateTripAsync(CreateTripDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} creating trip: {Title} for group {GroupId}",
            currentUserId, dto.Title, dto.GroupId);

        var group = await LoadGroupOrThrowAsync(dto.GroupId);
        await ValidateGroupMembershipAsync(dto.GroupId, currentUserId, "create a trip");

        if (dto.PlanningRangeStart.HasValue && dto.PlanningRangeStart <= DateOnly.FromDateTime(DateTime.UtcNow))
        {
            throw ErrorHelper.BadRequest("Planning range start date must be in the future.");
        }

        if (dto.PlanningRangeEnd.HasValue && dto.PlanningRangeEnd <= dto.PlanningRangeStart)
        {
            throw ErrorHelper.BadRequest("Planning range end date must be after start date.");
        }

        var trip = new Trip
        {
            GroupId = dto.GroupId,
            Title = dto.Title,
            Status = TripStatus.Setup,
            PlanningRangeStart = dto.PlanningRangeStart,
            PlanningRangeEnd = dto.PlanningRangeEnd,
            Location = dto.Location,
            Budget = dto.Budget,
            CreatedBy = currentUserId
        };

        await _unitOfWork.Trips.AddAsync(trip);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Trip {TripId} created successfully by user {UserId}",
            trip.Id, currentUserId);

        await _announcementService.NotifyTripCreatedAsync(trip.Id, trip.GroupId, trip.Title, currentUserId);

        return new TripDto
        {
            Id = trip.Id,
            GroupId = trip.GroupId,
            GroupName = group.Name,
            Title = trip.Title,
            Status = trip.Status,
            PlanningRangeStart = trip.PlanningRangeStart,
            PlanningRangeEnd = trip.PlanningRangeEnd,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            Location = trip.Location,
            Budget = trip.Budget,
            CreatedAt = trip.CreatedAt
        };
    }

    public async Task<TripDto> UpdateTripAsync(Guid tripId, UpdateTripDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} updating trip {TripId}",
            currentUserId, tripId);

        var trip = await LoadTripWithGroupOrThrowAsync(tripId);
        await ValidateGroupMembershipAsync(trip.GroupId, currentUserId, "update this trip");

        // Validate all changes before applying (similar to ActivityService pattern)
        if (dto.PlanningRangeStart.HasValue)
        {
            if (dto.PlanningRangeStart <= DateOnly.FromDateTime(DateTime.UtcNow))
            {
                throw ErrorHelper.BadRequest("Planning range start date must be in the future.");
            }
        }

        if (dto.PlanningRangeEnd.HasValue)
        {
            var effectivePlanningStart = dto.PlanningRangeStart ?? trip.PlanningRangeStart ?? DateOnly.FromDateTime(DateTime.UtcNow);
            if (dto.PlanningRangeEnd <= effectivePlanningStart)
            {
                throw ErrorHelper.BadRequest("Planning range end date must be after start date.");
            }
        }

        if (dto.StartDate.HasValue)
        {
            var startDateOnly = DateOnly.FromDateTime(dto.StartDate.Value);
            var effectivePlanningStart = dto.PlanningRangeStart ?? trip.PlanningRangeStart;
            var effectivePlanningEnd = dto.PlanningRangeEnd ?? trip.PlanningRangeEnd;

            if (effectivePlanningStart.HasValue && startDateOnly <= effectivePlanningStart)
            {
                throw ErrorHelper.BadRequest("Trip start date must be after planning range start date.");
            }
            if (effectivePlanningEnd.HasValue && startDateOnly >= effectivePlanningEnd)
            {
                throw ErrorHelper.BadRequest("Trip start date must be before planning range end date.");
            }
        }

        if (dto.EndDate.HasValue)
        {
            var effectiveStartDate = dto.StartDate ?? trip.StartDate;
            if (effectiveStartDate.HasValue && dto.EndDate <= effectiveStartDate)
            {
                throw ErrorHelper.BadRequest("Trip end date must be after start date.");
            }

            var endDateOnly = DateOnly.FromDateTime(dto.EndDate.Value);
            var effectivePlanningEnd = dto.PlanningRangeEnd ?? trip.PlanningRangeEnd;
            if (effectivePlanningEnd.HasValue && endDateOnly >= effectivePlanningEnd)
            {
                throw ErrorHelper.BadRequest("Trip end date must be before planning range end date.");
            }
        }

        // Apply updates after all validations pass
        if (!string.IsNullOrWhiteSpace(dto.Title))
        {
            trip.Title = dto.Title;
        }

        if (dto.PlanningRangeStart.HasValue)
        {
            trip.PlanningRangeStart = dto.PlanningRangeStart;
        }

        if (dto.PlanningRangeEnd.HasValue)
        {
            trip.PlanningRangeEnd = dto.PlanningRangeEnd;
        }

        if (dto.StartDate.HasValue)
        {
            trip.StartDate = dto.StartDate;
        }

        if (dto.EndDate.HasValue)
        {
            trip.EndDate = dto.EndDate;
        }

        if (dto.Location != null)
        {
            trip.Location = dto.Location;
        }

        if (dto.Settings != null)
        {
            trip.SettingsDetails = dto.Settings;
        }

        if (dto.Budget.HasValue)
        {
            trip.Budget = dto.Budget;
        }

        await _unitOfWork.Trips.Update(trip);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Trip {TripId} updated successfully", tripId);

        return new TripDto
        {
            Id = trip.Id,
            GroupId = trip.GroupId,
            GroupName = trip.Group.Name,
            Title = trip.Title,
            Status = trip.Status,
            PlanningRangeStart = trip.PlanningRangeStart,
            PlanningRangeEnd = trip.PlanningRangeEnd,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            Location = trip.Location,
            Budget = trip.Budget,
            CreatedAt = trip.CreatedAt
        };
    }

    public async Task<bool> DeleteTripAsync(Guid tripId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} deleting trip {TripId}",
            currentUserId, tripId);

        var trip = await LoadTripWithGroupOrThrowAsync(tripId);
        await ValidateGroupLeadershipAsync(trip.GroupId, currentUserId, "delete trips");

        await _unitOfWork.Trips.SoftRemove(trip);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Trip {TripId} deleted successfully", tripId);

        return true;
    }

    public async Task<TripDetailDto> GetTripDetailAsync(Guid tripId, PollStatus? pollStatus = null)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} getting trip detail {TripId}",
            currentUserId, tripId);

        var trip = await _unitOfWork.Trips.GetQueryable()
            .Include(t => t.Group)
            .Include(t => t.Expenses)
            .FirstOrDefaultAsync(t => t.Id == tripId && !t.IsDeleted);

        if (trip == null)
        {
            throw ErrorHelper.NotFound("The trip does not exist.");
        }

        await ValidateGroupMembershipAsync(trip.GroupId, currentUserId, "view this trip");

        var activities = await _unitOfWork.Activities.GetQueryable()
            .Where(a => a.TripId == tripId && !a.IsDeleted)
            .OrderBy(a => a.Date)
            .ThenBy(a => a.StartTime)
            .ThenBy(a => a.CreatedAt)
            .ToListAsync();

        var activityDtos = activities.Select(activity => new ActivityDto
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
            Latitude = activity.GeoCoordinates?.Y,
            Longitude = activity.GeoCoordinates?.X,
            LinkUrl = activity.LinkUrl,
            ImageUrl = activity.ImageUrl,
            Notes = activity.Notes,
            CreatedAt = activity.CreatedAt,
            UpdatedAt = activity.UpdatedAt
        }).ToList();

        var pollsQuery = _unitOfWork.Polls.GetQueryable()
            .Include(p => p.Options)
            .ThenInclude(o => o.Votes)
            .Where(p => p.TripId == tripId && !p.IsDeleted);

        if (pollStatus.HasValue)
        {
            pollsQuery = pollsQuery.Where(p => p.Status == pollStatus.Value);
        }

        var polls = await pollsQuery
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var creatorIds = polls.Select(p => p.CreatedBy).Distinct().ToList();
        var creators = await _unitOfWork.Users.GetQueryable()
            .Where(u => creatorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username);

        var pollDtos = polls.Select(poll => new PollDto
        {
            Id = poll.Id,
            TripId = poll.TripId,
            TripTitle = trip.Title,
            ActivityId = poll.ActivityId,
            Type = poll.Type,
            Title = poll.Title,
            Status = poll.Status,
            CreatedBy = poll.CreatedBy,
            CreatorName = creators.TryGetValue(poll.CreatedBy, out var username) ? username : "Unknown",
            CreatedAt = poll.CreatedAt,
            OptionCount = poll.Options.Count,
            TotalVotes = poll.Options.Sum(o => o.Votes.Count)
        }).ToList();

        var packingItems = await _unitOfWork.PackingItems.GetQueryable()
            .Where(pi => pi.TripId == tripId
                && !pi.IsDeleted
                && (pi.IsShared || pi.CreatedBy == currentUserId))
            .OrderBy(pi => pi.Category)
            .ThenBy(pi => pi.Name)
            .ToListAsync();

        var packingItemDtos = packingItems.Select(pi => new PackingItemDto
        {
            Id = pi.Id,
            TripId = pi.TripId,
            Name = pi.Name,
            Category = pi.Category,
            IsShared = pi.IsShared,
            IsChecked = pi.IsChecked,
            QuantityNeeded = pi.QuantityNeeded,
            CreatedAt = pi.CreatedAt,
            UpdatedAt = pi.UpdatedAt
        }).ToList();

        return new TripDetailDto
        {
            Id = trip.Id,
            GroupId = trip.GroupId,
            GroupName = trip.Group.Name,
            Title = trip.Title,
            Status = trip.Status,
            PlanningRangeStart = trip.PlanningRangeStart,
            PlanningRangeEnd = trip.PlanningRangeEnd,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            Location = trip.Location,
            Budget = trip.Budget,
            Settings = trip.SettingsDetails,
            CreatedAt = trip.CreatedAt,
            PollCount = pollDtos.Count,
            ActivityCount = activityDtos.Count,
            ExpenseCount = trip.Expenses.Count,
            Activities = activityDtos,
            Polls = pollDtos,
            PackingItems = packingItemDtos
        };
    }


    public async Task<Pagination<TripDto>> GetGroupTripsAsync(Guid groupId, TripQueryDto query)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} getting trips for group {GroupId}",
            currentUserId, groupId);

        var group = await LoadGroupOrThrowAsync(groupId);
        await ValidateGroupMembershipAsync(groupId, currentUserId, "view its trips");

        var tripsQuery = _unitOfWork.Trips.GetQueryable()
            .Where(t => t.GroupId == groupId && !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            tripsQuery = tripsQuery.Where(t =>
                t.Title.ToLower().Contains(searchTerm) ||
                group.Name.ToLower().Contains(searchTerm));
        }

        if (query.Status.HasValue)
        {
            tripsQuery = tripsQuery.Where(t => t.Status == query.Status.Value);
        }

        tripsQuery = query.SortBy switch
        {
            TripSortBy.StartDate => query.SortDescending
                ? tripsQuery.OrderByDescending(t => t.StartDate)
                : tripsQuery.OrderBy(t => t.StartDate),
            TripSortBy.PlanningRangeStart => query.SortDescending
                ? tripsQuery.OrderByDescending(t => t.PlanningRangeStart)
                : tripsQuery.OrderBy(t => t.PlanningRangeStart),
            _ => query.SortDescending
                ? tripsQuery.OrderByDescending(t => t.CreatedAt)
                : tripsQuery.OrderBy(t => t.CreatedAt)
        };

        var totalCount = await tripsQuery.CountAsync();

        var trips = await tripsQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var tripDtos = trips.Select(trip => new TripDto
        {
            Id = trip.Id,
            GroupId = trip.GroupId,
            GroupName = group.Name,
            Title = trip.Title,
            Status = trip.Status,
            PlanningRangeStart = trip.PlanningRangeStart,
            PlanningRangeEnd = trip.PlanningRangeEnd,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            Location = trip.Location,
            Budget = trip.Budget,
            CreatedAt = trip.CreatedAt
        }).ToList();

        return new Pagination<TripDto>(tripDtos, totalCount, query.PageNumber, query.PageSize);
    }

    public async Task<TripDto> UpdateTripStatusAsync(Guid tripId, TripStatus status)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} updating trip {TripId} status to {Status}",
            currentUserId, tripId, status);

        var trip = await LoadTripWithGroupOrThrowAsync(tripId);
        await ValidateGroupLeadershipAsync(trip.GroupId, currentUserId, "update trip status");

        trip.Status = status;

        await _unitOfWork.Trips.Update(trip);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Trip {TripId} status updated to {Status} successfully",
            tripId, status);

        return new TripDto
        {
            Id = trip.Id,
            GroupId = trip.GroupId,
            GroupName = trip.Group.Name,
            Title = trip.Title,
            Status = trip.Status,
            PlanningRangeStart = trip.PlanningRangeStart,
            PlanningRangeEnd = trip.PlanningRangeEnd,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            Location = trip.Location,
            Budget = trip.Budget,
            CreatedAt = trip.CreatedAt
        };
    }

    public async Task<Pagination<TripDto>> GetMyTripsAsync(TripQueryDto query)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} getting their trips", currentUserId);

        var groupIds = await _unitOfWork.GroupMembers.GetQueryable()
            .Where(gm => gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Active)
            .Select(gm => gm.GroupId)
            .ToListAsync();

        if (groupIds.Count == 0)
        {
            _loggerService.LogInformation("User {UserId} is not a member of any active groups",
                currentUserId);
            return new Pagination<TripDto>(new List<TripDto>(), 0, query.PageNumber, query.PageSize);
        }

        var tripsQuery = _unitOfWork.Trips.GetQueryable()
            .Include(t => t.Group)
            .Where(t => groupIds.Contains(t.GroupId) && !t.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            tripsQuery = tripsQuery.Where(t =>
                t.Title.ToLower().Contains(searchTerm) ||
                t.Group.Name.ToLower().Contains(searchTerm));
        }

        if (query.Status.HasValue)
        {
            tripsQuery = tripsQuery.Where(t => t.Status == query.Status.Value);
        }

        tripsQuery = query.SortBy switch
        {
            TripSortBy.StartDate => query.SortDescending
                ? tripsQuery.OrderByDescending(t => t.StartDate)
                : tripsQuery.OrderBy(t => t.StartDate),
            TripSortBy.PlanningRangeStart => query.SortDescending
                ? tripsQuery.OrderByDescending(t => t.PlanningRangeStart)
                : tripsQuery.OrderBy(t => t.PlanningRangeStart),
            _ => query.SortDescending
                ? tripsQuery.OrderByDescending(t => t.CreatedAt)
                : tripsQuery.OrderBy(t => t.CreatedAt)
        };

        var totalCount = await tripsQuery.CountAsync();

        var trips = await tripsQuery
            .Skip((query.PageNumber - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync();

        var tripDtos = trips.Select(trip => new TripDto
        {
            Id = trip.Id,
            GroupId = trip.GroupId,
            GroupName = trip.Group.Name,
            Title = trip.Title,
            Status = trip.Status,
            PlanningRangeStart = trip.PlanningRangeStart,
            PlanningRangeEnd = trip.PlanningRangeEnd,
            StartDate = trip.StartDate,
            EndDate = trip.EndDate,
            Location = trip.Location,
            Budget = trip.Budget,
            CreatedAt = trip.CreatedAt
        }).ToList();

        return new Pagination<TripDto>(tripDtos, totalCount, query.PageNumber, query.PageSize);
    }

    #region Authorization Helpers

    private async Task<Group> LoadGroupOrThrowAsync(Guid groupId)
    {
        var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
        if (group == null)
        {
            throw ErrorHelper.NotFound("The group does not exist.");
        }
        return group;
    }

    private async Task<Trip> LoadTripWithGroupOrThrowAsync(Guid tripId)
    {
        var trip = await _unitOfWork.Trips.GetQueryable()
            .Include(t => t.Group)
            .FirstOrDefaultAsync(t => t.Id == tripId && !t.IsDeleted);

        if (trip == null)
        {
            throw ErrorHelper.NotFound("The trip does not exist.");
        }

        return trip;
    }

    private async Task ValidateGroupMembershipAsync(Guid groupId, Guid userId, string action)
    {
        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == groupId
                && gm.UserId == userId
                && gm.Status == GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden($"You must be a member of the group to {action}.");
        }
    }

    private async Task ValidateGroupLeadershipAsync(Guid groupId, Guid userId, string action)
    {
        var isLeader = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == groupId
                && gm.UserId == userId
                && gm.Status == GroupMemberStatus.Active
                && gm.Role == GroupMemberRole.Leader);

        if (!isLeader)
        {
            throw ErrorHelper.Forbidden($"Only group leaders can {action}.");
        }
    }

    #endregion
}
