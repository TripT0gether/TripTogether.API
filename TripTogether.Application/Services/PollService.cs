using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TripTogether.Application.DTOs.PollDTO;
using TripTogether.Application.Helpers;
using TripTogether.Application.Interfaces;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Services;

public sealed class PollService : IPollService
{
    private const int MaxActivitiesPerDay = 10;
    private const string TimeFormat = "HH:mm";

    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly ILogger _loggerService;

    public PollService(
        IUnitOfWork unitOfWork,
        IClaimsService claimsService,
        ILogger<PollService> loggerService)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _loggerService = loggerService;
    }

    private static void ValidatePollOptionForType(PollType pollType, CreatePollOptionDto optionDto)
    {
        switch (pollType)
        {
            case PollType.Date:
                // Date polls: ONLY StartDate, EndDate (optional) - NO times
                if (!string.IsNullOrWhiteSpace(optionDto.TextValue))
                {
                    throw ErrorHelper.BadRequest("Date polls cannot have TextValue. Use StartDate and EndDate instead.");
                }
                if (optionDto.StartTime.HasValue || optionDto.EndTime.HasValue)
                {
                    throw ErrorHelper.BadRequest("Date polls cannot have StartTime or EndTime. Use a separate Time poll to set activity times.");
                }
                if (optionDto.StartDate == null)
                {
                    throw ErrorHelper.BadRequest("Date poll options must have a start date.");
                }
                if (optionDto.StartDate < DateOnly.FromDateTime(DateTime.UtcNow))
                {
                    throw ErrorHelper.BadRequest("Poll option start date must be in the future.");
                }
                if (optionDto.EndDate != null && optionDto.EndDate < optionDto.StartDate)
                {
                    throw ErrorHelper.BadRequest("Poll option end date cannot be before start date.");
                }
                break;

            case PollType.Time:
                // Time polls: ONLY TextValue (for time description) and/or StartTime/EndTime
                if (optionDto.StartDate != null || optionDto.EndDate != null)
                {
                    throw ErrorHelper.BadRequest("Time polls cannot have StartDate or EndDate. Use StartTime and EndTime instead.");
                }
                if (string.IsNullOrWhiteSpace(optionDto.TextValue) && optionDto.StartTime == null)
                {
                    throw ErrorHelper.BadRequest("Time poll options must have either TextValue (time description) or StartTime.");
                }
                break;

            case PollType.Destination:
                // Destination polls: ONLY TextValue (destination name)
                if (optionDto.StartDate != null || optionDto.EndDate != null)
                {
                    throw ErrorHelper.BadRequest("Destination polls cannot have StartDate or EndDate.");
                }
                if (optionDto.TimeOfDay != null)
                {
                    throw ErrorHelper.BadRequest("Destination polls cannot have TimeOfDay.");
                }
                if (string.IsNullOrWhiteSpace(optionDto.TextValue))
                {
                    throw ErrorHelper.BadRequest("Destination poll options must have a TextValue (destination name).");
                }
                break;

            case PollType.Budget:
                // Budget polls: ONLY Budget (decimal value)
                if (optionDto.StartDate != null || optionDto.EndDate != null)
                {
                    throw ErrorHelper.BadRequest("Budget polls cannot have StartDate or EndDate.");
                }
                if (optionDto.TimeOfDay != null)
                {
                    throw ErrorHelper.BadRequest("Budget polls cannot have TimeOfDay.");
                }
                if (!string.IsNullOrWhiteSpace(optionDto.TextValue))
                {
                    throw ErrorHelper.BadRequest("Budget polls cannot have TextValue. Use Budget property instead.");
                }
                if (!optionDto.Budget.HasValue)
                {
                    throw ErrorHelper.BadRequest("Budget poll options must have a Budget value.");
                }
                if (optionDto.Budget.Value <= 0)
                {
                    throw ErrorHelper.BadRequest("Budget must be a positive value.");
                }
                break;

            default:
                throw ErrorHelper.BadRequest($"Unknown poll type: {pollType}");
        }
    }

    public async Task<PollDto> CreatePollAsync(CreatePollDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} creating poll: {Title} for trip {TripId}",
            currentUserId, dto.Title, dto.TripId);

        var trip = await LoadTripWithGroupAsync(dto.TripId);
        await ValidateGroupMembershipAsync(trip.GroupId, currentUserId, "create a poll");

        await ValidateActivityIfProvidedAsync(dto.TripId, dto.ActivityId);

        switch (dto.Type)
        {
            case PollType.Budget:
                if (dto.ActivityId.HasValue)
                {
                    throw ErrorHelper.BadRequest("Budget polls can only be created for trips, not for individual activities.");
                }
                break;

            case PollType.Destination:
                if (!dto.ActivityId.HasValue)
                {
                    throw ErrorHelper.BadRequest("Destination polls can only be created for activities, not for trips. Use a trip-level poll for overall trip destination.");
                }
                break;
        }

        await ValidateNoDuplicatePollAsync(dto.TripId, dto.ActivityId, dto.Type);

        var poll = new Poll
        {
            TripId = dto.TripId,
            ActivityId = dto.ActivityId,
            Type = dto.Type,
            Title = dto.Title,
            Status = PollStatus.Open,
            CreatedBy = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.Polls.AddAsync(poll);

        foreach (var optionDto in dto.Options)
        {
            // Validate poll option based on poll type
            ValidatePollOptionForType(dto.Type, optionDto);

            var option = new PollOption
            {
                PollId = poll.Id,
                TextValue = optionDto.TextValue,
                Budget = optionDto.Budget,
                StartDate = optionDto.StartDate,
                EndDate = optionDto.EndDate,
                StartTime = optionDto.StartTime,
                EndTime = optionDto.EndTime,
                TimeOfDay = optionDto.TimeOfDay,
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.PollOptions.AddAsync(option);
        }

        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Poll {PollId} created successfully by user {UserId}",
            poll.Id, currentUserId);

        var creator = await _unitOfWork.Users.GetByIdAsync(currentUserId);

        return new PollDto
        {
            Id = poll.Id,
            TripId = poll.TripId,
            TripTitle = trip.Title,
            ActivityId = poll.ActivityId,
            Type = poll.Type,
            Title = poll.Title,
            Status = poll.Status,
            CreatedBy = poll.CreatedBy,
            CreatorName = creator?.Username ?? "Unknown",
            CreatedAt = poll.CreatedAt,
            OptionCount = dto.Options.Count,
            TotalVotes = 0
        };
    }

    public async Task<PollDto> UpdatePollAsync(Guid pollId, UpdatePollDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} updating poll {pollId}");

        var poll = await _unitOfWork.Polls.GetQueryable()
            .Include(p => p.Trip)
            .ThenInclude(t => t.Group)
            .ThenInclude(g => g.Members)
            .Include(p => p.Options)
            .ThenInclude(o => o.Votes)
            .FirstOrDefaultAsync(p => p.Id == pollId && !p.IsDeleted);

        if (poll == null)
        {
            throw ErrorHelper.NotFound("The poll does not exist.");
        }

        var groupMember = poll.Trip.Group.Members.FirstOrDefault(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (groupMember == null)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to update this poll.");
        }

        if (poll.CreatedBy != currentUserId && groupMember.Role != GroupMemberRole.Leader)
        {
            throw ErrorHelper.Forbidden("Only the poll creator or group leaders can update polls.");
        }

        if (!string.IsNullOrWhiteSpace(dto.Title))
        {
            poll.Title = dto.Title;
        }

        poll.UpdatedAt = DateTime.UtcNow;
        poll.UpdatedBy = currentUserId;

        await _unitOfWork.Polls.Update(poll);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Poll {pollId} updated successfully");

        var creator = await _unitOfWork.Users.GetByIdAsync(poll.CreatedBy);

        return new PollDto
        {
            Id = poll.Id,
            TripId = poll.TripId,
            TripTitle = poll.Trip.Title,
            ActivityId = poll.ActivityId,
            Type = poll.Type,
            Title = poll.Title,
            Status = poll.Status,
            CreatedBy = poll.CreatedBy,
            CreatorName = creator?.Username ?? "Unknown",
            CreatedAt = poll.CreatedAt,
            OptionCount = poll.Options.Count,
            TotalVotes = poll.Options.Sum(o => o.Votes.Count)
        };
    }

    public async Task<bool> DeletePollAsync(Guid pollId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} deleting poll {pollId}");

        var poll = await _unitOfWork.Polls.GetQueryable()
            .Include(p => p.Trip)
            .ThenInclude(t => t.Group)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(p => p.Id == pollId && !p.IsDeleted);

        if (poll == null)
        {
            throw ErrorHelper.NotFound("The poll does not exist.");
        }

        var groupMember = poll.Trip.Group.Members.FirstOrDefault(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (groupMember == null)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to delete this poll.");
        }

        if (poll.CreatedBy != currentUserId && groupMember.Role != GroupMemberRole.Leader)
        {
            throw ErrorHelper.Forbidden("Only the poll creator or group leaders can delete polls.");
        }

        await _unitOfWork.Polls.SoftRemove(poll);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Poll {pollId} deleted successfully");

        return true;
    }

    public async Task<PollDetailDto> GetPollDetailAsync(Guid pollId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} getting poll detail {pollId}");

        var poll = await _unitOfWork.Polls.GetQueryable()
            .Include(p => p.Trip)
            .ThenInclude(t => t.Group)
            .ThenInclude(g => g.Members)
            .Include(p => p.Options)
            .ThenInclude(o => o.Votes)
            .FirstOrDefaultAsync(p => p.Id == pollId && !p.IsDeleted);

        if (poll == null)
        {
            throw ErrorHelper.NotFound("The poll does not exist.");
        }

        var isGroupMember = poll.Trip.Group.Members.Any(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to view this poll.");
        }

        var creator = await _unitOfWork.Users.GetByIdAsync(poll.CreatedBy);

        return new PollDetailDto
        {
            Id = poll.Id,
            TripId = poll.TripId,
            TripTitle = poll.Trip.Title,
            ActivityId = poll.ActivityId,
            Type = poll.Type,
            Title = poll.Title,
            Status = poll.Status,
            CreatedBy = poll.CreatedBy,
            CreatorName = creator?.Username ?? "Unknown",
            CreatedAt = poll.CreatedAt,
            Options = poll.Options.Select(o => new PollOptionDto
            {
                Id = o.Id,
                PollId = o.PollId,
                TextValue = o.TextValue,
                Budget = o.Budget,
                StartDate = o.StartDate,
                EndDate = o.EndDate,
                StartTime = o.StartTime,
                EndTime = o.EndTime,
                TimeOfDay = o.TimeOfDay,
                VoteCount = o.Votes.Count,
                CreatedAt = o.CreatedAt
            }).ToList(),
            TotalVotes = poll.Options.Sum(o => o.Votes.Count)
        };
    }

    public async Task<Pagination<PollDto>> GetPollsAsync(Guid tripId, PollScope scope = PollScope.All, int pageNumber = 1, int pageSize = 10)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} getting {scope} polls for trip {tripId}");

        var trip = await _unitOfWork.Trips.GetQueryable()
            .Include(t => t.Group)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(t => t.Id == tripId && !t.IsDeleted);

        if (trip == null)
        {
            throw ErrorHelper.NotFound("The trip does not exist.");
        }

        var isGroupMember = trip.Group.Members.Any(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to view polls.");
        }

        var pollsQuery = _unitOfWork.Polls.GetQueryable()
            .Include(p => p.Trip)
            .Include(p => p.Options)
            .ThenInclude(o => o.Votes)
            .Where(p => p.TripId == tripId && !p.IsDeleted);

        pollsQuery = scope switch
        {
            PollScope.TripOnly => pollsQuery.Where(p => p.ActivityId == null),
            PollScope.ActivityOnly => pollsQuery.Where(p => p.ActivityId != null),
            _ => pollsQuery
        };

        var totalCount = await pollsQuery.CountAsync();

        var polls = await pollsQuery
            .OrderByDescending(p => p.CreatedAt)
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var creatorIds = polls.Select(p => p.CreatedBy).Distinct().ToList();
        var creators = await _unitOfWork.Users.GetQueryable()
            .Where(u => creatorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username);

        var pollDtos = polls.Select(poll => new PollDto
        {
            Id = poll.Id,
            TripId = poll.TripId,
            TripTitle = poll.Trip.Title,
            ActivityId = poll.ActivityId,
            Type = poll.Type,
            Title = poll.Title,
            Status = poll.Status,
            CreatedBy = poll.CreatedBy,
            CreatorName = creators.ContainsKey(poll.CreatedBy) ? creators[poll.CreatedBy] : "Unknown",
            CreatedAt = poll.CreatedAt,
            OptionCount = poll.Options.Count,
            TotalVotes = poll.Options.Sum(o => o.Votes.Count)
        }).ToList();

        return new Pagination<PollDto>(pollDtos, totalCount, pageNumber, pageSize);
    }

    public async Task<PollDto> ClosePollAsync(Guid pollId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} closing poll {pollId}");

        var poll = await _unitOfWork.Polls.GetQueryable()
            .Include(p => p.Trip)
            .ThenInclude(t => t.Group)
            .ThenInclude(g => g.Members)
            .Include(p => p.Options)
            .ThenInclude(o => o.Votes)
            .FirstOrDefaultAsync(p => p.Id == pollId && !p.IsDeleted);

        if (poll == null)
        {
            throw ErrorHelper.NotFound("The poll does not exist.");
        }

        var groupMember = poll.Trip.Group.Members.FirstOrDefault(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (groupMember == null)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to close this poll.");
        }

        if (poll.CreatedBy != currentUserId && groupMember.Role != GroupMemberRole.Leader)
        {
            throw ErrorHelper.Forbidden("Only the poll creator or group leaders can close polls.");
        }

        poll.Status = PollStatus.Closed;
        poll.UpdatedAt = DateTime.UtcNow;
        poll.UpdatedBy = currentUserId;

        await _unitOfWork.Polls.Update(poll);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Poll {pollId} closed successfully");

        var creator = await _unitOfWork.Users.GetByIdAsync(poll.CreatedBy);

        return new PollDto
        {
            Id = poll.Id,
            TripId = poll.TripId,
            TripTitle = poll.Trip.Title,
            ActivityId = poll.ActivityId,
            Type = poll.Type,
            Title = poll.Title,
            Status = poll.Status,
            CreatedBy = poll.CreatedBy,
            CreatorName = creator?.Username ?? "Unknown",
            CreatedAt = poll.CreatedAt,
            OptionCount = poll.Options.Count,
            TotalVotes = poll.Options.Sum(o => o.Votes.Count)
        };
    }

    public async Task<PollDto> FinalizePollAsync(FinalizePollDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} finalizing poll {dto.PollId} with option {dto.SelectedOptionId}");

        var poll = await _unitOfWork.Polls.GetQueryable()
            .Include(p => p.Trip)
            .ThenInclude(t => t.Group)
            .ThenInclude(g => g.Members)
            .Include(p => p.Activity)
            .Include(p => p.Options)
            .ThenInclude(o => o.Votes)
            .FirstOrDefaultAsync(p => p.Id == dto.PollId && !p.IsDeleted);

        if (poll == null)
        {
            throw ErrorHelper.NotFound("The poll does not exist.");
        }

        var groupMember = poll.Trip.Group.Members.FirstOrDefault(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (groupMember == null)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to finalize this poll.");
        }

        if (groupMember.Role != GroupMemberRole.Leader)
        {
            throw ErrorHelper.Forbidden("Only group leaders can finalize polls.");
        }

        if (poll.Status == PollStatus.Finalized)
        {
            throw ErrorHelper.BadRequest("This poll has already been finalized.");
        }

        var selectedOption = poll.Options.FirstOrDefault(o => o.Id == dto.SelectedOptionId);
        if (selectedOption == null)
        {
            throw ErrorHelper.BadRequest("The selected option does not belong to this poll.");
        }

        // Update poll status to Finalized
        poll.Status = PollStatus.Finalized;
        poll.UpdatedAt = DateTime.UtcNow;
        poll.UpdatedBy = currentUserId;

        // Handle finalization based on poll type
        switch (poll.Type)
        {
            case PollType.Date:
                await FinalizeDatePoll(poll, selectedOption, currentUserId);
                break;

            case PollType.Time:
                await FinalizeTimePoll(poll, selectedOption);
                break;

            case PollType.Destination:
                await FinalizeDestinationPoll(poll, selectedOption);
                break;

            case PollType.Budget:
                await FinalizeBudgetPoll(poll, selectedOption);
                break;

            default:
                throw ErrorHelper.BadRequest($"Poll type {poll.Type} cannot be finalized.");
        }

        await _unitOfWork.Polls.Update(poll);
        await _unitOfWork.SaveChangesAsync();

        var creator = await _unitOfWork.Users.GetByIdAsync(poll.CreatedBy);

        return new PollDto
        {
            Id = poll.Id,
            TripId = poll.TripId,
            TripTitle = poll.Trip.Title,
            ActivityId = poll.ActivityId,
            Type = poll.Type,
            Title = poll.Title,
            Status = poll.Status,
            CreatedBy = poll.CreatedBy,
            CreatorName = creator?.Username ?? "Unknown",
            CreatedAt = poll.CreatedAt,
            OptionCount = poll.Options.Count,
            TotalVotes = poll.Options.Sum(o => o.Votes.Count)
        };
    }

    private async Task FinalizeDatePoll(Poll poll, PollOption selectedOption, Guid currentUserId)
    {
        if (!selectedOption.StartDate.HasValue)
        {
            throw ErrorHelper.BadRequest("The selected option does not have a valid date.");
        }

        if (poll.ActivityId.HasValue && poll.Activity != null)
        {
            await FinalizeActivityDatePoll(poll, selectedOption);
        }
        else
        {
            await FinalizeTripDatePoll(poll, selectedOption);
        }
    }

    private async Task FinalizeActivityDatePoll(Poll poll, PollOption selectedOption)
    {
        var activity = poll.Activity!;
        var targetDate = selectedOption.StartDate!.Value;
        var oldDate = activity.Date;

        // Validate capacity for the target date
        if (activity.Date != targetDate)
        {
            var existingActivitiesCount = await _unitOfWork.Activities.GetQueryable()
                .CountAsync(a => a.TripId == poll.TripId
                    && a.Date == targetDate
                    && !a.IsDeleted
                    && a.Id != activity.Id);

            if (existingActivitiesCount >= MaxActivitiesPerDay)
            {
                throw ErrorHelper.BadRequest($"Cannot finalize poll: Maximum of {MaxActivitiesPerDay} activities per day has been reached for the selected date.");
            }
        }

        // Update activity date
        activity.Date = targetDate;
        activity.Status = ActivityStatus.Scheduled;

        await _unitOfWork.Activities.Update(activity);

        // Reorder activities on affected dates
        if (oldDate != targetDate)
        {
            if (oldDate.HasValue)
            {
                await ReorderActivitiesOnDateAsync(poll.TripId, oldDate.Value);
            }
            await ReorderActivitiesOnDateAsync(poll.TripId, targetDate);
        }
        else if (oldDate.HasValue)
        {
            await ReorderActivitiesOnDateAsync(poll.TripId, oldDate.Value);
        }

        _loggerService.LogInformation("Date poll {PollId} finalized successfully. Activity {ActivityId} scheduled for {Date}",
            poll.Id, activity.Id, activity.Date);
    }

    private async Task FinalizeTripDatePoll(Poll poll, PollOption selectedOption)
    {
        var startDate = selectedOption.StartDate!.Value;
        var endDate = selectedOption.EndDate;

        if (!endDate.HasValue)
        {
            throw ErrorHelper.BadRequest("Cannot finalize trip dates: Selected option must have both start and end dates for trip-level polls.");
        }

        if (endDate.Value <= startDate)
        {
            throw ErrorHelper.BadRequest("Trip end date must be after start date.");
        }

        var trip = poll.Trip;

        // Validate that selected dates fall within planning range (if set)
        ValidateDateWithinPlanningRange(trip, startDate, endDate.Value);

        // Set the finalized trip dates (ensure UTC for PostgreSQL)
        trip.StartDate = DateTime.SpecifyKind(startDate.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);
        trip.EndDate = DateTime.SpecifyKind(endDate.Value.ToDateTime(TimeOnly.MinValue), DateTimeKind.Utc);

        await _unitOfWork.Trips.Update(trip);

        _loggerService.LogInformation("Date poll {PollId} finalized successfully. Trip {TripId} dates updated to {StartDate} - {EndDate}",
            poll.Id, trip.Id, trip.StartDate, trip.EndDate);
    }

    private void ValidateDateWithinPlanningRange(Trip trip, DateOnly startDate, DateOnly endDate)
    {
        if (trip.PlanningRangeStart.HasValue && startDate < trip.PlanningRangeStart.Value)
        {
            throw ErrorHelper.BadRequest($"Selected start date {startDate} is before the planning range start {trip.PlanningRangeStart.Value}.");
        }

        if (trip.PlanningRangeEnd.HasValue && endDate > trip.PlanningRangeEnd.Value)
        {
            throw ErrorHelper.BadRequest($"Selected end date {endDate} is after the planning range end {trip.PlanningRangeEnd.Value}.");
        }
    }

    private async Task FinalizeTimePoll(Poll poll, PollOption selectedOption)
    {
        TimeOnly? newStartTime = selectedOption.StartTime;
        TimeOnly? newEndTime = selectedOption.EndTime;

        // Fallback to parsing TextValue if times not provided directly
        if (!newStartTime.HasValue && !string.IsNullOrWhiteSpace(selectedOption.TextValue))
        {
            var timeParts = selectedOption.TextValue.Split('-', StringSplitOptions.TrimEntries);

            if (timeParts.Length >= 1 && TimeOnly.TryParse(timeParts[0], out var parsedStartTime))
            {
                newStartTime = parsedStartTime;

                if (timeParts.Length >= 2 && TimeOnly.TryParse(timeParts[1], out var parsedEndTime))
                {
                    newEndTime = parsedEndTime;
                }
            }
            else
            {
                throw ErrorHelper.BadRequest("The selected time option does not have a valid time format. Expected format: 'HH:mm' or 'HH:mm - HH:mm'.");
            }
        }

        if (!newStartTime.HasValue)
        {
            throw ErrorHelper.BadRequest("The selected time option does not have a valid start time.");
        }

        TimeSlotHelper.ValidateTimeLogic(newStartTime, newEndTime);

        if (poll.ActivityId.HasValue && poll.Activity != null)
        {
            await FinalizeActivityTimePoll(poll, newStartTime.Value, newEndTime);
        }
        else
        {
            await FinalizeTripTimePoll(poll, newStartTime.Value, newEndTime);
        }
    }

    private async Task FinalizeActivityTimePoll(Poll poll, TimeOnly startTime, TimeOnly? endTime)
    {
        var activity = poll.Activity!;

        if (!activity.Date.HasValue)
        {
            throw ErrorHelper.BadRequest("Cannot finalize time poll: Activity must have a date set first. Please finalize a date poll before setting times.");
        }

        // Check for time conflicts with existing activities on the same date
        if (endTime.HasValue)
        {
            var existingActivities = await _unitOfWork.Activities.GetQueryable()
                .Where(a => a.TripId == poll.TripId
                    && a.Date == activity.Date.Value
                    && !a.IsDeleted
                    && a.Id != activity.Id
                    && a.StartTime.HasValue
                    && a.EndTime.HasValue)
                .ToListAsync();

            foreach (var existing in existingActivities)
            {
                ValidateNoTimeConflicts(startTime, endTime.Value, existing);
            }
        }

        activity.StartTime = startTime;
        activity.EndTime = endTime;

        if (activity.Status == ActivityStatus.Idea)
        {
            activity.Status = ActivityStatus.Scheduled;
        }

        await _unitOfWork.Activities.Update(activity);

        await ReorderActivitiesOnDateAsync(poll.TripId, activity.Date.Value);

        _loggerService.LogInformation("Time poll {PollId} finalized successfully. Activity {ActivityId} time set to {StartTime}{EndTime}",
            poll.Id, activity.Id, activity.StartTime, endTime.HasValue ? $" - {endTime}" : "");
    }

    private async Task FinalizeTripTimePoll(Poll poll, TimeOnly startTime, TimeOnly? endTime)
    {
        var trip = poll.Trip;

        if (!trip.StartDate.HasValue)
        {
            throw ErrorHelper.BadRequest("Cannot finalize time poll: Trip must have a start date set first. Please finalize a date poll before setting times.");
        }

        // Update trip start time (departure time) - ensure UTC for PostgreSQL
        var startDate = DateOnly.FromDateTime(trip.StartDate.Value);
        trip.StartDate = DateTime.SpecifyKind(startDate.ToDateTime(startTime), DateTimeKind.Utc);

        // Update trip end time (return time) if provided and end date exists
        if (endTime.HasValue && trip.EndDate.HasValue)
        {
            var endDate = DateOnly.FromDateTime(trip.EndDate.Value);
            trip.EndDate = DateTime.SpecifyKind(endDate.ToDateTime(endTime.Value), DateTimeKind.Utc);
        }

        await _unitOfWork.Trips.Update(trip);

        _loggerService.LogInformation("Time poll {PollId} finalized successfully. Trip {TripId} departure/return times updated to {StartTime}{EndTime}",
            poll.Id, trip.Id, trip.StartDate, endTime.HasValue && trip.EndDate.HasValue ? $" - {trip.EndDate}" : "");
    }

    private async Task FinalizeDestinationPoll(Poll poll, PollOption selectedOption)
    {
        if (poll.ActivityId.HasValue && poll.Activity != null)
        {
            var activity = poll.Activity;

            // Set LocationName from the selected option
            if (!string.IsNullOrWhiteSpace(selectedOption.TextValue))
            {
                activity.LocationName = selectedOption.TextValue;
                _loggerService.LogInformation($"Destination poll {poll.Id} finalized successfully. Activity {activity.Id} location set to {activity.LocationName}");
            }
            else
            {
                throw ErrorHelper.BadRequest("The selected destination option does not have a valid location name.");
            }

            await _unitOfWork.Activities.Update(activity);
        }
        else
        {
            throw ErrorHelper.BadRequest("Destination polls can only be finalized for activities, not for trips.");
        }
    }

    private async Task FinalizeBudgetPoll(Poll poll, PollOption selectedOption)
    {
        if (poll.ActivityId.HasValue)
        {
            throw ErrorHelper.BadRequest("Budget polls can only be finalized for trips, not for activities.");
        }

        if (!selectedOption.Budget.HasValue)
        {
            throw ErrorHelper.BadRequest("The selected budget option does not have a valid budget value.");
        }

        if (selectedOption.Budget.Value <= 0)
        {
            throw ErrorHelper.BadRequest("Budget must be a positive value.");
        }

        var trip = poll.Trip;
        trip.Budget = selectedOption.Budget.Value;
        await _unitOfWork.Trips.Update(trip);

        _loggerService.LogInformation("Budget poll {PollId} finalized successfully. Trip {TripId} budget set to {Budget}",
            poll.Id, trip.Id, trip.Budget);
    }

    public async Task<PollOptionDto> AddPollOptionAsync(Guid pollId, CreatePollOptionDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} adding option to poll {pollId}");

        var poll = await _unitOfWork.Polls.GetQueryable()
        .Include(p => p.Trip)
        .ThenInclude(t => t.Group)
        .ThenInclude(g => g.Members)
        .FirstOrDefaultAsync(p => p.Id == pollId && !p.IsDeleted);

        if (poll == null)
        {
            throw ErrorHelper.NotFound("The poll does not exist.");
        }

        var isGroupMember = poll.Trip.Group.Members.Any(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to add options.");
        }

        if (poll.Status == PollStatus.Closed || poll.Status == PollStatus.Finalized)
        {
            throw ErrorHelper.BadRequest("Cannot add options to a closed or finalized poll.");
        }

        // Validate poll option based on poll type
        ValidatePollOptionForType(poll.Type, dto);

        var option = new PollOption
        {
            PollId = pollId,
            TextValue = dto.TextValue,
            Budget = dto.Budget,
            StartDate = dto.StartDate,
            EndDate = dto.EndDate,
            StartTime = dto.StartTime,
            EndTime = dto.EndTime,
            TimeOfDay = dto.TimeOfDay,
            CreatedBy = currentUserId,
            CreatedAt = DateTime.UtcNow
        };

        await _unitOfWork.PollOptions.AddAsync(option);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Option {option.Id} added to poll {pollId} successfully");

        return new PollOptionDto
        {
            Id = option.Id,
            PollId = option.PollId,
            TextValue = option.TextValue,
            Budget = option.Budget,
            StartDate = option.StartDate,
            EndDate = option.EndDate,
            StartTime = option.StartTime,
            EndTime = option.EndTime,
            TimeOfDay = option.TimeOfDay,
            VoteCount = 0,
            CreatedAt = option.CreatedAt
        };
    }

    public async Task<bool> RemovePollOptionAsync(Guid optionId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} removing poll option {optionId}");

        var option = await _unitOfWork.PollOptions.GetQueryable()
            .Include(o => o.Poll)
            .ThenInclude(p => p.Trip)
            .ThenInclude(t => t.Group)
            .ThenInclude(g => g.Members)
            .FirstOrDefaultAsync(o => o.Id == optionId && !o.IsDeleted);

        if (option == null)
        {
            throw ErrorHelper.NotFound("The poll option does not exist.");
        }

        var groupMember = option.Poll.Trip.Group.Members.FirstOrDefault(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (groupMember == null)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to remove options.");
        }

        if (option.Poll.Status == PollStatus.Closed || option.Poll.Status == PollStatus.Finalized)
        {
            throw ErrorHelper.BadRequest("Cannot remove options from a closed or finalized poll.");
        }

        if (option.CreatedBy != currentUserId && groupMember.Role != GroupMemberRole.Leader)
        {
            throw ErrorHelper.Forbidden("Only the option creator or group leaders can remove options.");
        }

        await _unitOfWork.PollOptions.SoftRemove(option);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Poll option {optionId} removed successfully");

        return true;
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

        var sortedActivities = activities
            .OrderBy(a => a.StartTime.HasValue ? 0 : 1)
            .ThenBy(a => a.StartTime)
            .ToList();

        for (int i = 0; i < sortedActivities.Count; i++)
        {
            sortedActivities[i].ScheduleDayIndex = i + 1;
            await _unitOfWork.Activities.Update(sortedActivities[i]);
        }
    }

    #region Authorization Helpers

    private async Task<Trip> LoadTripWithGroupAsync(Guid tripId)
    {
        var trip = await _unitOfWork.Trips.GetQueryable()
            .Include(t => t.Group)
            .ThenInclude(g => g.Members)
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

    private async Task<GroupMember> GetGroupMemberOrThrowAsync(Guid groupId, Guid userId, string action)
    {
        var groupMember = await _unitOfWork.GroupMembers.GetQueryable()
            .FirstOrDefaultAsync(gm => gm.GroupId == groupId
                && gm.UserId == userId
                && gm.Status == GroupMemberStatus.Active);

        if (groupMember == null)
        {
            throw ErrorHelper.Forbidden($"You must be a member of the group to {action}.");
        }

        return groupMember;
    }

    private void ValidateGroupLeadership(GroupMember groupMember, string action)
    {
        if (groupMember.Role != GroupMemberRole.Leader)
        {
            throw ErrorHelper.Forbidden($"Only group leaders can {action}.");
        }
    }

    #endregion

    #region Validation Helpers

    private static void ValidatePollTypeForScope(PollType pollType, Guid? activityId)
    {

    }

    private async Task ValidateActivityIfProvidedAsync(Guid tripId, Guid? activityId)
    {
        if (!activityId.HasValue)
        {
            return;
        }

        var activity = await _unitOfWork.Activities.GetByIdAsync(activityId.Value);
        if (activity == null || activity.TripId != tripId)
        {
            throw ErrorHelper.BadRequest("The specified activity does not exist or does not belong to this trip.");
        }
    }

    private async Task ValidateNoDuplicatePollAsync(Guid tripId, Guid? activityId, PollType pollType)
    {
        var existingPoll = await _unitOfWork.Polls.GetQueryable()
            .AnyAsync(p => p.TripId == tripId
                && p.ActivityId == activityId
                && p.Type == pollType
                && (p.Status == PollStatus.Open || p.Status == PollStatus.Closed)
                && !p.IsDeleted);

        if (existingPoll)
        {
            var scope = activityId.HasValue ? "activity" : "trip";
            throw ErrorHelper.Conflict($"A {pollType} poll already exists for this {scope}. Each {scope} can only have one poll per type.");
        }
    }

    private void ValidateNoTimeConflicts(TimeOnly newStartTime, TimeOnly? newEndTime, Activity existing)
    {
        if (!existing.StartTime.HasValue || !existing.EndTime.HasValue || !newEndTime.HasValue)
        {
            return;
        }

        bool hasConflict = (newStartTime < existing.EndTime.Value && newStartTime >= existing.StartTime.Value) ||
                          (newEndTime.Value > existing.StartTime.Value && newEndTime.Value <= existing.EndTime.Value) ||
                          (newStartTime <= existing.StartTime.Value && newEndTime.Value >= existing.EndTime.Value);

        if (hasConflict)
        {
            throw ErrorHelper.BadRequest(
                $"Activity time conflicts with existing activity '{existing.Title}' ({existing.StartTime.Value.ToString(TimeFormat)} - {existing.EndTime.Value.ToString(TimeFormat)}).");
        }
    }

    #endregion
}
