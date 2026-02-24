using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TripTogether.Application.DTOs.PollDTO;
using TripTogether.Application.Helpers;
using TripTogether.Application.Interfaces;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Services;

public sealed class PollService : IPollService
{
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

    private static bool IsValidJson(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return true;
        }

        try
        {
            JsonDocument.Parse(json);
            return true;
        }
        catch (JsonException)
        {
            return false;
        }
    }

    private static void ValidatePollOptionForType(PollType pollType, CreatePollOptionDto optionDto)
    {
        switch (pollType)
        {
            case PollType.Date:
                // Date polls: ONLY DateStart, DateEnd (optional), TimeOfDay (optional)
                if (!string.IsNullOrWhiteSpace(optionDto.TextValue))
                {
                    throw ErrorHelper.BadRequest("Date polls cannot have TextValue. Use DateStart and DateEnd instead.");
                }
                if (!string.IsNullOrWhiteSpace(optionDto.MediaUrl))
                {
                    throw ErrorHelper.BadRequest("Date polls cannot have MediaUrl.");
                }
                if (!string.IsNullOrWhiteSpace(optionDto.Metadata))
                {
                    throw ErrorHelper.BadRequest("Date polls cannot have Metadata.");
                }
                if (optionDto.DateStart == null)
                {
                    throw ErrorHelper.BadRequest("Date poll options must have a start date.");
                }
                if (optionDto.DateStart < DateTime.UtcNow)
                {
                    throw ErrorHelper.BadRequest("Poll option start date must be in the future.");
                }
                if (optionDto.DateEnd != null && optionDto.DateEnd < optionDto.DateStart)
                {
                    throw ErrorHelper.BadRequest("Poll option end date cannot be before start date.");
                }
                break;

            case PollType.Time:
                // Time polls: ONLY TextValue (for time description) and/or TimeOfDay
                if (optionDto.DateStart != null || optionDto.DateEnd != null)
                {
                    throw ErrorHelper.BadRequest("Time polls cannot have DateStart or DateEnd. Use TimeOfDay instead.");
                }
                if (!string.IsNullOrWhiteSpace(optionDto.MediaUrl))
                {
                    throw ErrorHelper.BadRequest("Time polls cannot have MediaUrl.");
                }
                if (!string.IsNullOrWhiteSpace(optionDto.Metadata))
                {
                    throw ErrorHelper.BadRequest("Time polls cannot have Metadata.");
                }
                if (string.IsNullOrWhiteSpace(optionDto.TextValue) && optionDto.TimeOfDay == null)
                {
                    throw ErrorHelper.BadRequest("Time poll options must have either TextValue (time description) or TimeOfDay.");
                }
                break;

            case PollType.Destination:
                // Destination polls: ONLY TextValue (destination name), MediaUrl (optional)
                if (optionDto.DateStart != null || optionDto.DateEnd != null)
                {
                    throw ErrorHelper.BadRequest("Destination polls cannot have DateStart or DateEnd.");
                }
                if (optionDto.TimeOfDay != null)
                {
                    throw ErrorHelper.BadRequest("Destination polls cannot have TimeOfDay.");
                }
                if (!string.IsNullOrWhiteSpace(optionDto.Metadata))
                {
                    throw ErrorHelper.BadRequest("Destination polls cannot have Metadata.");
                }
                if (string.IsNullOrWhiteSpace(optionDto.TextValue))
                {
                    throw ErrorHelper.BadRequest("Destination poll options must have a TextValue (destination name).");
                }
                break;

            case PollType.Budget:
                // Budget polls: ONLY Metadata (for budget range as JSON)
                if (optionDto.DateStart != null || optionDto.DateEnd != null)
                {
                    throw ErrorHelper.BadRequest("Budget polls cannot have DateStart or DateEnd.");
                }
                if (optionDto.TimeOfDay != null)
                {
                    throw ErrorHelper.BadRequest("Budget polls cannot have TimeOfDay.");
                }
                if (!string.IsNullOrWhiteSpace(optionDto.TextValue))
                {
                    throw ErrorHelper.BadRequest("Budget polls cannot have TextValue. Use Metadata for budget range (JSON format).");
                }
                if (!string.IsNullOrWhiteSpace(optionDto.MediaUrl))
                {
                    throw ErrorHelper.BadRequest("Budget polls cannot have MediaUrl.");
                }
                if (string.IsNullOrWhiteSpace(optionDto.Metadata))
                {
                    throw ErrorHelper.BadRequest("Budget poll options must have Metadata (budget range in JSON format).");
                }
                if (!IsValidJson(optionDto.Metadata))
                {
                    throw ErrorHelper.BadRequest("Budget poll Metadata must be valid JSON.");
                }
                break;

            default:
                throw ErrorHelper.BadRequest($"Unknown poll type: {pollType}");
        }
    }

    public async Task<PollDto> CreatePollAsync(CreatePollDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} creating poll: {dto.Title} for trip {dto.TripId}");

        var trip = await _unitOfWork.Trips.GetQueryable()
        .Include(t => t.Group)
        .ThenInclude(g => g.Members)
        .FirstOrDefaultAsync(t => t.Id == dto.TripId);

        if (trip == null)
        {
            throw ErrorHelper.NotFound("The trip does not exist.");
        }

        var isGroupMember = trip.Group.Members.Any(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to create a poll.");
        }

        if (dto.ActivityId.HasValue)
        {
            var activity = await _unitOfWork.Activities.GetByIdAsync(dto.ActivityId.Value);
            if (activity == null || activity.TripId != dto.TripId)
            {
                throw ErrorHelper.BadRequest("The specified activity does not exist or does not belong to this trip.");
            }

            // Check if an activity-level poll of this type already exists
            var existingActivityPoll = await _unitOfWork.Polls.GetQueryable()
                .AnyAsync(p => p.ActivityId == dto.ActivityId.Value
                    && p.Type == dto.Type
                    && (p.Status == PollStatus.Open || p.Status == PollStatus.Closed));

            if (existingActivityPoll)
            {
                throw ErrorHelper.Conflict($"A {dto.Type} poll already exists for this activity. Each activity can only have one poll per type.");
            }
        }
        else
        {
            // Check if a trip-level poll of this type already exists
            var existingTripPoll = await _unitOfWork.Polls.GetQueryable()
                .AnyAsync(p => p.TripId == dto.TripId
                    && p.ActivityId == null
                    && p.Type == dto.Type
                    && (p.Status == PollStatus.Open || p.Status == PollStatus.Closed));

            if (existingTripPoll)
            {
                throw ErrorHelper.Conflict($"A {dto.Type} poll already exists for this trip. Each trip can only have one poll per type.");
            }
        }

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
                Index = optionDto.Index,
                TextValue = optionDto.TextValue,
                MediaUrl = optionDto.MediaUrl,
                Metadata = optionDto.Metadata,
                DateStart = optionDto.DateStart,
                DateEnd = optionDto.DateEnd,
                TimeOfDay = optionDto.TimeOfDay,
                CreatedBy = currentUserId,
                CreatedAt = DateTime.UtcNow
            };
            await _unitOfWork.PollOptions.AddAsync(option);
        }

        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation($"Poll {poll.Id} created successfully by user {currentUserId}");

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
            .FirstOrDefaultAsync(p => p.Id == pollId);

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

        if (dto.Status.HasValue)
        {
            poll.Status = dto.Status.Value;
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
            .FirstOrDefaultAsync(p => p.Id == pollId);

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
            .FirstOrDefaultAsync(p => p.Id == pollId);

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
                Index = o.Index,
                TextValue = o.TextValue,
                MediaUrl = o.MediaUrl,
                Metadata = o.Metadata,
                DateStart = o.DateStart,
                DateEnd = o.DateEnd,
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
            .FirstOrDefaultAsync(t => t.Id == tripId);

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
            .Where(p => p.TripId == tripId);

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
            .FirstOrDefaultAsync(p => p.Id == pollId);

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
            .FirstOrDefaultAsync(p => p.Id == dto.PollId);

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
        if (!selectedOption.DateStart.HasValue)
        {
            throw ErrorHelper.BadRequest("The selected option does not have a valid date.");
        }

        if (poll.ActivityId.HasValue && poll.Activity != null)
        {
            var activity = poll.Activity;
            var targetDate = DateOnly.FromDateTime(selectedOption.DateStart.Value);

            if (activity.Date != targetDate)
            {
                var activitiesOnDate = await _unitOfWork.Activities.GetQueryable()
                    .CountAsync(a => a.TripId == poll.TripId && a.Date == targetDate);

                if (activitiesOnDate >= 10)
                {
                    throw ErrorHelper.BadRequest("Cannot finalize poll: Maximum of 10 activities per day has been reached for the selected date.");
                }
            }

            // Prepare new values for validation
            TimeOnly? newStartTime = null;
            TimeOnly? newEndTime = null;

            if (selectedOption.DateStart.Value.TimeOfDay != TimeSpan.Zero || selectedOption.DateEnd.HasValue)
            {
                newStartTime = TimeOnly.FromDateTime(selectedOption.DateStart.Value);
                if (selectedOption.DateEnd.HasValue)
                {
                    newEndTime = TimeOnly.FromDateTime(selectedOption.DateEnd.Value);
                }
            }

            TimeSlotHelper.ValidateTimeLogic(newStartTime, newEndTime, selectedOption.TimeOfDay);

            activity.Date = targetDate;
            activity.StartTime = newStartTime;
            activity.EndTime = newEndTime;

            if (selectedOption.TimeOfDay.HasValue)
            {
                activity.ScheduleSlot = selectedOption.TimeOfDay;
            }

            // ScheduleDayIndex should be set manually or remain null
            // It represents the order of activities within the same day (1-10), not which day of the trip

            activity.Status = ActivityStatus.Scheduled;
            await _unitOfWork.Activities.Update(activity);

            _loggerService.LogInformation($"Date poll {poll.Id} finalized successfully. Activity {activity.Id} scheduled for {activity.Date}");
        }
        else
        {
            var startDate = selectedOption.DateStart.Value;
            var endDate = selectedOption.DateEnd;

            if (endDate.HasValue && endDate.Value <= startDate)
            {
                throw ErrorHelper.BadRequest("Trip end date must be after start date.");
            }

            if (!endDate.HasValue)
            {
                throw ErrorHelper.BadRequest("Cannot finalize trip dates: Selected option must have both start and end dates for trip-level polls.");
            }

            var trip = poll.Trip;
            var startDateOnly = DateOnly.FromDateTime(startDate);
            var endDateOnly = DateOnly.FromDateTime(endDate.Value);

            // Validate that selected dates fall within or can extend the planning range
            if (trip.PlanningRangeStart.HasValue)
            {
                if (startDateOnly < trip.PlanningRangeStart.Value)
                {
                    throw ErrorHelper.BadRequest($"Selected start date {startDateOnly} is before the planning range start {trip.PlanningRangeStart.Value}.");
                }
            }

            if (trip.PlanningRangeEnd.HasValue)
            {
                if (endDateOnly > trip.PlanningRangeEnd.Value)
                {
                    throw ErrorHelper.BadRequest($"Selected end date {endDateOnly} is after the planning range end {trip.PlanningRangeEnd.Value}.");
                }
            }

            // If no planning range is set, create one with buffer
            if (!trip.PlanningRangeStart.HasValue)
            {
                trip.PlanningRangeStart = startDateOnly.AddDays(-1);
            }

            if (!trip.PlanningRangeEnd.HasValue)
            {
                trip.PlanningRangeEnd = endDateOnly.AddDays(1);
            }

            trip.StartDate = startDate;
            trip.EndDate = endDate.Value;

            await _unitOfWork.Trips.Update(trip);

            _loggerService.LogInformation($"Date poll {poll.Id} finalized successfully. Trip {trip.Id} dates updated to {trip.StartDate} - {trip.EndDate}");
        }
    }

    private async Task FinalizeTimePoll(Poll poll, PollOption selectedOption)
    {
        if (poll.ActivityId.HasValue && poll.Activity != null)
        {
            var activity = poll.Activity;

            // Set ScheduleSlot from the selected option
            if (selectedOption.TimeOfDay.HasValue)
            {
                activity.ScheduleSlot = selectedOption.TimeOfDay;
                _loggerService.LogInformation($"Time poll {poll.Id} finalized successfully. Activity {activity.Id} time slot set to {activity.ScheduleSlot}");
            }
            else
            {
                throw ErrorHelper.BadRequest("The selected time option does not have a valid TimeOfDay value.");
            }

            await _unitOfWork.Activities.Update(activity);
        }
        else
        {
            throw ErrorHelper.BadRequest("Time polls can only be finalized for activities, not for trips.");
        }
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

        var trip = poll.Trip;

        // Parse budget from Metadata (should be JSON with budget information)
        if (string.IsNullOrWhiteSpace(selectedOption.Metadata))
        {
            throw ErrorHelper.BadRequest("The selected budget option does not have valid budget data.");
        }

        try
        {
            var budgetData = JsonDocument.Parse(selectedOption.Metadata);

            // Try to get budget value from different possible JSON structures
            decimal budgetValue = 0;

            if (budgetData.RootElement.TryGetProperty("budget", out var budgetProp))
            {
                budgetValue = budgetProp.GetDecimal();
            }
            else if (budgetData.RootElement.TryGetProperty("max", out var maxProp))
            {
                budgetValue = maxProp.GetDecimal();
            }
            else if (budgetData.RootElement.TryGetProperty("amount", out var amountProp))
            {
                budgetValue = amountProp.GetDecimal();
            }
            else
            {
                throw ErrorHelper.BadRequest("Budget metadata must contain 'budget', 'max', or 'amount' property.");
            }

            if (budgetValue <= 0)
            {
                throw ErrorHelper.BadRequest("Budget must be a positive value.");
            }

            trip.Budget = budgetValue;
            await _unitOfWork.Trips.Update(trip);

            _loggerService.LogInformation($"Budget poll {poll.Id} finalized successfully. Trip {trip.Id} budget set to {trip.Budget}");
        }
        catch (JsonException ex)
        {
            throw ErrorHelper.BadRequest($"Invalid budget metadata format: {ex.Message}");
        }
    }

    public async Task<PollOptionDto> AddPollOptionAsync(Guid pollId, CreatePollOptionDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} adding option to poll {pollId}");

        var poll = await _unitOfWork.Polls.GetQueryable()
       .Include(p => p.Trip)
   .ThenInclude(t => t.Group)
        .ThenInclude(g => g.Members)
      .FirstOrDefaultAsync(p => p.Id == pollId);

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
            Index = dto.Index,
            TextValue = dto.TextValue,
            MediaUrl = dto.MediaUrl,
            Metadata = dto.Metadata,
            DateStart = dto.DateStart,
            DateEnd = dto.DateEnd,
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
            Index = option.Index,
            TextValue = option.TextValue,
            MediaUrl = option.MediaUrl,
            Metadata = option.Metadata,
            DateStart = option.DateStart,
            DateEnd = option.DateEnd,
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
        .FirstOrDefaultAsync(o => o.Id == optionId);

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
}
