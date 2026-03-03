using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

        _loggerService.LogInformation($"User {currentUserId} creating trip: {dto.Title} for group {dto.GroupId}");

        var group = await _unitOfWork.Groups.GetByIdAsync(dto.GroupId);
        if (group == null)
        {
            throw ErrorHelper.NotFound("The group does not exist.");
        }

        var groupMember = await _unitOfWork.GroupMembers.GetQueryable()
            .FirstOrDefaultAsync(gm => gm.GroupId == dto.GroupId
                && gm.UserId == currentUserId
                && gm.Status == GroupMemberStatus.Active);

        if (groupMember == null)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to create a trip.");
        }

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
            Status = TripStatus.Planning,
            PlanningRangeStart = dto.PlanningRangeStart,
            PlanningRangeEnd = dto.PlanningRangeEnd,
            Budget = dto.Budget,
            CreatedBy = currentUserId
        };

        await _unitOfWork.Trips.AddAsync(trip);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Trip {trip.Id} created successfully by user {currentUserId}");

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
            Budget = trip.Budget,
            CreatedAt = trip.CreatedAt
        };
    }

    public async Task<TripDto> UpdateTripAsync(Guid tripId, UpdateTripDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} updating trip {tripId}");

        var trip = await _unitOfWork.Trips.GetQueryable()
          .Include(t => t.Group)
          .FirstOrDefaultAsync(t => t.Id == tripId && !t.IsDeleted);

        if (trip == null)
        {
            throw ErrorHelper.NotFound("The trip does not exist.");
        }

        var groupMember = await _unitOfWork.GroupMembers.GetQueryable()
                 .FirstOrDefaultAsync(gm => gm.GroupId == trip.GroupId
                     && gm.UserId == currentUserId
         && gm.Status == GroupMemberStatus.Active);

        if (groupMember == null)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to update this trip.");
        }

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

        _loggerService.LogInformation($"Trip {tripId} updated successfully");

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
            Budget = trip.Budget,
            CreatedAt = trip.CreatedAt
        };
    }

    public async Task<bool> DeleteTripAsync(Guid tripId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} deleting trip {tripId}");

        var trip = await _unitOfWork.Trips.GetQueryable()
            .Include(t => t.Group)
            .FirstOrDefaultAsync(t => t.Id == tripId && !t.IsDeleted);

        if (trip == null)
        {
            throw ErrorHelper.NotFound("The trip does not exist.");
        }

        var groupMember = await _unitOfWork.GroupMembers.GetQueryable()
            .FirstOrDefaultAsync(gm => gm.GroupId == trip.GroupId
                && gm.UserId == currentUserId
                && gm.Status == GroupMemberStatus.Active
                && gm.Role == GroupMemberRole.Leader);

        if (groupMember == null)
        {
            throw ErrorHelper.Forbidden("Only group leaders can delete trips.");
        }

        await _unitOfWork.Trips.SoftRemove(trip);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Trip {tripId} deleted successfully");

        return true;
    }

    public async Task<TripDetailDto> GetTripDetailAsync(Guid tripId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} getting trip detail {tripId}");

        var trip = await _unitOfWork.Trips.GetQueryable()
            .Include(t => t.Group)
            .Include(t => t.Polls)
            .Include(t => t.Activities)
            .Include(t => t.Expenses)
            .FirstOrDefaultAsync(t => t.Id == tripId && !t.IsDeleted);

        if (trip == null)
        {
            throw ErrorHelper.NotFound("The trip does not exist.");
        }

        var groupMember = await _unitOfWork.GroupMembers.GetQueryable()
            .FirstOrDefaultAsync(gm => gm.GroupId == trip.GroupId
                && gm.UserId == currentUserId
                && gm.Status == GroupMemberStatus.Active);

        if (groupMember == null)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to view this trip.");
        }

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
            Budget = trip.Budget,
            Settings = trip.SettingsDetails,
            CreatedAt = trip.CreatedAt,
            PollCount = trip.Polls.Count,
            ActivityCount = trip.Activities.Count,
            ExpenseCount = trip.Expenses.Count
        };
    }


    public async Task<Pagination<TripDto>> GetGroupTripsAsync(Guid groupId, TripQueryDto query)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} getting trips for group {groupId}");

        var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
        if (group == null)
        {
            throw ErrorHelper.NotFound("The group does not exist.");
        }

        var groupMember = await _unitOfWork.GroupMembers.GetQueryable()
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId
                && gm.UserId == currentUserId
                && gm.Status == GroupMemberStatus.Active);

        if (groupMember == null)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to view its trips.");
        }

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
            Budget = trip.Budget,
            CreatedAt = trip.CreatedAt
        }).ToList();

        return new Pagination<TripDto>(tripDtos, totalCount, query.PageNumber, query.PageSize);
    }

    public async Task<TripDto> UpdateTripStatusAsync(Guid tripId, TripStatus status)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} updating trip {tripId} status to {status}");

        var trip = await _unitOfWork.Trips.GetQueryable()
            .Include(t => t.Group)
            .FirstOrDefaultAsync(t => t.Id == tripId && !t.IsDeleted);

        if (trip == null)
        {
            throw ErrorHelper.NotFound("The trip does not exist.");
        }

        var groupMember = await _unitOfWork.GroupMembers.GetQueryable()
            .FirstOrDefaultAsync(gm => gm.GroupId == trip.GroupId
                && gm.UserId == currentUserId
                && gm.Status == GroupMemberStatus.Active
                && gm.Role == GroupMemberRole.Leader);

        if (groupMember == null)
        {
            throw ErrorHelper.Forbidden("Only group leaders can update trip status.");
        }

        trip.Status = status;

        await _unitOfWork.Trips.Update(trip);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Trip {tripId} status updated to {status} successfully");

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
            Budget = trip.Budget,
            CreatedAt = trip.CreatedAt
        };
    }

    public async Task<Pagination<TripDto>> GetMyTripsAsync(TripQueryDto query)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} getting their trips");

        var groupIds = await _unitOfWork.GroupMembers.GetQueryable()
            .Where(gm => gm.UserId == currentUserId && gm.Status == GroupMemberStatus.Active)
            .Select(gm => gm.GroupId)
            .ToListAsync();

        if (groupIds.Count == 0)
        {
            _loggerService.LogInformation($"User {currentUserId} is not a member of any active groups");
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
            Budget = trip.Budget,
            CreatedAt = trip.CreatedAt
        }).ToList();

        return new Pagination<TripDto>(tripDtos, totalCount, query.PageNumber, query.PageSize);
    }
}
