using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using TripTogether.Application.DTOs.PollDTO;
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
            if (string.IsNullOrWhiteSpace(optionDto.TextValue) &&
             string.IsNullOrWhiteSpace(optionDto.MediaUrl) &&
           string.IsNullOrWhiteSpace(optionDto.Metadata) &&
               optionDto.DateStart == null &&
                optionDto.DateEnd == null &&
             optionDto.TimeOfDay == null)
            {
                throw ErrorHelper.BadRequest("Poll option must have at least one valid value.");
            }

            if (!IsValidJson(optionDto.Metadata))
            {
                throw ErrorHelper.BadRequest("Poll option metadata must be valid JSON.");
            }

            // For Date polls, validate date range
            if (dto.Type == PollType.Date)
            {
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
            }

            var option = new PollOption
            {
                PollId = poll.Id,
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

    public async Task<PollDto> FinalizeDatePollAsync(FinalizeDatePollDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} finalizing date poll {dto.PollId} with option {dto.SelectedOptionId}");

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

        if (poll.Type != PollType.Date)
        {
            throw ErrorHelper.BadRequest("Only Date polls can be finalized with this method.");
        }

        var groupMember = poll.Trip.Group.Members.FirstOrDefault(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (groupMember == null)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to finalize this poll.");
        }

        if (groupMember.Role != GroupMemberRole.Leader)
        {
            throw ErrorHelper.Forbidden("Only group leaders can finalize date polls.");
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

        if (!selectedOption.DateStart.HasValue)
        {
            throw ErrorHelper.BadRequest("The selected option does not have a valid date.");
        }

        // Update poll status to Finalized
        poll.Status = PollStatus.Finalized;
        poll.UpdatedAt = DateTime.UtcNow;
        poll.UpdatedBy = currentUserId;

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

            // Utilize shared validation logic
            ValidateTimeLogic(newStartTime, newEndTime, selectedOption.TimeOfDay);

            activity.Date = targetDate;
            activity.StartTime = newStartTime;
            activity.EndTime = newEndTime;

            if (selectedOption.TimeOfDay.HasValue)
            {
                activity.ScheduleSlot = selectedOption.TimeOfDay;
            }

            activity.Status = ActivityStatus.Scheduled;
            await _unitOfWork.Activities.Update(activity);

            _loggerService.LogInformation($"Date poll {dto.PollId} finalized successfully. Activity {activity.Id} scheduled for {activity.Date}");
        }
        else
        {
            // Validate dates for Trip update
            var startDate = selectedOption.DateStart.Value;
            var endDate = selectedOption.DateEnd;

            if (endDate.HasValue && endDate.Value <= startDate)
            {
                throw ErrorHelper.BadRequest("Trip end date must be after start date.");
            }
            if (!endDate.HasValue)
            {
                 // Decide policy for single-date options on trips.
                 // Assuming trips must be multi-day or explicitly set end > start.
                 // For now, if no end date provided in option, we cannot create a valid Trip range (Start < End).
                 // We reject it.
                 throw ErrorHelper.BadRequest("Cannot finalize trip dates: Selected option has no end date, but trips require a duration.");
            }

            // TripService invariants: StartDate > PlanningRangeStart AND EndDate < PlanningRangeEnd
            // We set PlanningRange to accommodate the dates with 1-day buffer.
            var trip = poll.Trip;

            trip.PlanningRangeStart = DateOnly.FromDateTime(startDate).AddDays(-1); 
            trip.PlanningRangeEnd = DateOnly.FromDateTime(endDate.Value).AddDays(1);

            trip.StartDate = startDate;
            trip.EndDate = endDate.Value;

            await _unitOfWork.Trips.Update(trip);

            _loggerService.LogInformation($"Date poll {dto.PollId} finalized successfully. Trip {trip.Id} dates updated to {trip.StartDate} - {trip.EndDate}");
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

        if (!IsValidJson(dto.Metadata))
        {
            throw ErrorHelper.BadRequest("Poll option metadata must be valid JSON.");
        }

        // For Date polls, validate date range
        if (poll.Type == PollType.Date)
        {
            if (dto.DateStart == null)
            {
                throw ErrorHelper.BadRequest("Date poll options must have a start date.");
            }

            if (dto.DateStart < DateTime.UtcNow)
            {
                throw ErrorHelper.BadRequest("Poll option start date must be in the future.");
            }

            if (dto.DateEnd != null && dto.DateEnd < dto.DateStart)
            {
                throw ErrorHelper.BadRequest("Poll option end date cannot be before start date.");
            }
        }

        var option = new PollOption
        {
            PollId = pollId,
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
}
