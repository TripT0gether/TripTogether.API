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

        var poll = new Poll
        {
            TripId = dto.TripId,
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

            if (optionDto.DateEnd != null && optionDto.DateStart == null)
            {
                throw ErrorHelper.BadRequest("Poll option need both start and end");
            }
            else if (optionDto.DateEnd == null && optionDto.DateStart != null)
            {
                throw ErrorHelper.BadRequest("Poll option need both start and end");
            }


            if (
                (optionDto.DateStart.HasValue && trip.PlanningRangeStart.HasValue && optionDto.DateStart.Value.Date < trip.PlanningRangeStart.Value.ToDateTime(TimeOnly.MinValue)) ||
                (optionDto.DateEnd.HasValue && trip.PlanningRangeEnd.HasValue && optionDto.DateEnd.Value.Date > trip.PlanningRangeEnd.Value.ToDateTime(TimeOnly.MaxValue))
            )
            {
                throw ErrorHelper.BadRequest("Poll option dates must be within the trip's planning range.");
            }


            if (optionDto.DateStart != null && (optionDto.DateStart < DateTime.UtcNow || optionDto.DateStart > optionDto.DateEnd))
            {
                throw ErrorHelper.BadRequest("Poll option start date cannot be later than end date.");
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

        var isGroupMember = poll.Trip.Group.Members.Any(m => m.UserId == currentUserId && m.Status == GroupMemberStatus.Active);
        if (!isGroupMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to update this poll.");
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

    public async Task<List<PollDto>> GetTripPollsAsync(Guid tripId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation($"User {currentUserId} getting polls for trip {tripId}");

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

        var polls = await _unitOfWork.Polls.GetQueryable()
            .Include(p => p.Trip)
            .Include(p => p.Options)
            .ThenInclude(o => o.Votes)
            .Where(p => p.TripId == tripId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

        var creatorIds = polls.Select(p => p.CreatedBy).Distinct().ToList();
        var creators = await _unitOfWork.Users.GetQueryable()
            .Where(u => creatorIds.Contains(u.Id))
            .ToDictionaryAsync(u => u.Id, u => u.Username);

        return polls.Select(poll => new PollDto
        {
            Id = poll.Id,
            TripId = poll.TripId,
            TripTitle = poll.Trip.Title,
            Type = poll.Type,
            Title = poll.Title,
            Status = poll.Status,
            CreatedBy = poll.CreatedBy,
            CreatorName = creators.ContainsKey(poll.CreatedBy) ? creators[poll.CreatedBy] : "Unknown",
            CreatedAt = poll.CreatedAt,
            OptionCount = poll.Options.Count,
            TotalVotes = poll.Options.Sum(o => o.Votes.Count)
        }).ToList();
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

        if (poll.Status == PollStatus.Closed)
        {
            throw ErrorHelper.BadRequest("Cannot add options to a closed poll.");
        }

        if (!IsValidJson(dto.Metadata))
        {
            throw ErrorHelper.BadRequest("Poll option metadata must be valid JSON.");
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

        if (option.Poll.Status == PollStatus.Closed)
        {
            throw ErrorHelper.BadRequest("Cannot remove options from a closed poll.");
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
