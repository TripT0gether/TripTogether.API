using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TripTogether.Application.DTOs.PackingAssignmentDTO;
using TripTogether.Application.Interfaces;

namespace TripTogether.Application.Services;

public sealed class PackingAssignmentService : IPackingAssignmentService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly ILogger<PackingAssignmentService> _loggerService;

    public PackingAssignmentService(
        IUnitOfWork unitOfWork,
        IClaimsService claimsService,
        ILogger<PackingAssignmentService> loggerService)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _loggerService = loggerService;
    }

    public async Task<PackingAssignmentDto> CreateAssignmentAsync(CreatePackingAssignmentDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} creating packing assignment for item {PackingItemId}",
            currentUserId, dto.PackingItemId);

        var packingItem = await _unitOfWork.PackingItems.GetQueryable()
            .Include(pi => pi.Trip)
            .FirstOrDefaultAsync(pi => pi.Id == dto.PackingItemId);

        if (packingItem == null)
        {
            throw ErrorHelper.NotFound("The packing item does not exist.");
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == packingItem.Trip.GroupId
                && gm.UserId == currentUserId
                && gm.Status == Domain.Enums.GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to create packing assignments.");
        }

        // If UserId is null, assign to current user (assign to myself)
        var targetUserId = dto.UserId ?? currentUserId;

        // Validate target user exists and is a member
        var assignedUser = await _unitOfWork.Users.GetByIdAsync(targetUserId);
        if (assignedUser == null)
        {
            throw ErrorHelper.NotFound("The assigned user does not exist.");
        }

        var isAssignedUserMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == packingItem.Trip.GroupId
                && gm.UserId == targetUserId
                && gm.Status == Domain.Enums.GroupMemberStatus.Active);

        if (!isAssignedUserMember)
        {
            throw ErrorHelper.BadRequest("The assigned user must be a member of the trip's group.");
        }

        // Validate quantity based on IsShared and QuantityNeeded
        if (packingItem.IsShared)
        {
            // For shared items, check total assigned quantity doesn't exceed QuantityNeeded
            var currentlyAssigned = await _unitOfWork.PackingAssignments.GetQueryable()
                .Where(pa => pa.PackingItemId == dto.PackingItemId)
                .SumAsync(pa => pa.Quantity);

            var totalAfterAssignment = currentlyAssigned + dto.Quantity;

            if (totalAfterAssignment > packingItem.QuantityNeeded)
            {
                var remaining = packingItem.QuantityNeeded - currentlyAssigned;
                throw ErrorHelper.BadRequest(
                    $"Cannot assign {dto.Quantity} items. This shared item needs {packingItem.QuantityNeeded} total, " +
                    $"currently {currentlyAssigned} assigned. Only {remaining} remaining to assign.");
            }
        }
        else
        {
            // For personal items, check if user already has an assignment
            var existingAssignment = await _unitOfWork.PackingAssignments.GetQueryable()
                .AnyAsync(pa => pa.PackingItemId == dto.PackingItemId && pa.UserId == targetUserId);

            if (existingAssignment)
            {
                throw ErrorHelper.BadRequest(
                    "This is a personal item and the user already has an assignment for it. " +
                    "Please update the existing assignment instead.");
            }

            // Personal items typically have Quantity = 1 per person
            if (dto.Quantity != 1)
            {
                _loggerService.LogWarning(
                    "Personal item assigned with Quantity={Quantity}. Typically personal items have Quantity=1", 
                    dto.Quantity);
            }
        }

        var assignment = new PackingAssignment
        {
            PackingItemId = dto.PackingItemId,
            UserId = targetUserId,
            Quantity = dto.Quantity,
            IsChecked = false
        };

        await _unitOfWork.PackingAssignments.AddAsync(assignment);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Packing assignment {AssignmentId} created successfully", assignment.Id);

        return await MapToDtoAsync(assignment);
    }

    public async Task<PackingAssignmentDto> UpdateAssignmentAsync(Guid assignmentId, UpdatePackingAssignmentDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} updating packing assignment {AssignmentId}",
            currentUserId, assignmentId);

        var assignment = await _unitOfWork.PackingAssignments.GetQueryable()
            .Include(pa => pa.PackingItem)
                .ThenInclude(pi => pi.Trip)
            .FirstOrDefaultAsync(pa => pa.Id == assignmentId);

        if (assignment == null)
        {
            throw ErrorHelper.NotFound("The packing assignment does not exist.");
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == assignment.PackingItem.Trip.GroupId
                && gm.UserId == currentUserId
                && gm.Status == Domain.Enums.GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to update packing assignments.");
        }

        // Check ownership or leadership
        var isOwner = assignment.UserId == currentUserId;
        
        var isLeader = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == assignment.PackingItem.Trip.GroupId
                && gm.UserId == currentUserId
                && gm.Role == Domain.Enums.GroupMemberRole.Leader
                && gm.Status == Domain.Enums.GroupMemberStatus.Active);

        if (!isOwner && !isLeader)
        {
            throw ErrorHelper.Forbidden(
                "You can only update your own assignments. Trip leaders can update any assignment.");
        }

        // Validate quantity update based on IsShared and QuantityNeeded
        if (dto.Quantity.HasValue && dto.Quantity.Value != assignment.Quantity)
        {
            if (assignment.PackingItem.IsShared)
            {
                // For shared items, check total assigned quantity doesn't exceed QuantityNeeded
                var currentlyAssigned = await _unitOfWork.PackingAssignments.GetQueryable()
                    .Where(pa => pa.PackingItemId == assignment.PackingItemId && pa.Id != assignmentId)
                    .SumAsync(pa => pa.Quantity);

                var totalAfterUpdate = currentlyAssigned + dto.Quantity.Value;

                if (totalAfterUpdate > assignment.PackingItem.QuantityNeeded)
                {
                    var remaining = assignment.PackingItem.QuantityNeeded - currentlyAssigned;
                    throw ErrorHelper.BadRequest(
                        $"Cannot update to {dto.Quantity.Value} items. This shared item needs {assignment.PackingItem.QuantityNeeded} total, " +
                        $"currently {currentlyAssigned} assigned by others. Only {remaining} available for this assignment.");
                }
            }
            else
            {
                // Personal items warning
                if (dto.Quantity.Value != 1)
                {
                    _loggerService.LogWarning(
                        "Personal item quantity updated to {Quantity}. Typically personal items have Quantity=1", 
                        dto.Quantity.Value);
                }
            }
        }

        if (dto.Quantity.HasValue) assignment.Quantity = dto.Quantity.Value;
        if (dto.IsChecked.HasValue) assignment.IsChecked = dto.IsChecked.Value;

        await _unitOfWork.PackingAssignments.Update(assignment);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Packing assignment {AssignmentId} updated successfully", assignmentId);

        return await MapToDtoAsync(assignment);
    }

    public async Task<bool> DeleteAssignmentAsync(Guid assignmentId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} deleting packing assignment {AssignmentId}",
            currentUserId, assignmentId);

        var assignment = await _unitOfWork.PackingAssignments.GetQueryable()
            .Include(pa => pa.PackingItem)
                .ThenInclude(pi => pi.Trip)
            .FirstOrDefaultAsync(pa => pa.Id == assignmentId);

        if (assignment == null)
        {
            throw ErrorHelper.NotFound("The packing assignment does not exist.");
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == assignment.PackingItem.Trip.GroupId
                && gm.UserId == currentUserId
                && gm.Status == Domain.Enums.GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to delete packing assignments.");
        }

        // Check ownership or leadership
        var isOwner = assignment.UserId == currentUserId;
        
        var isLeader = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == assignment.PackingItem.Trip.GroupId
                && gm.UserId == currentUserId
                && gm.Role == Domain.Enums.GroupMemberRole.Leader
                && gm.Status == Domain.Enums.GroupMemberStatus.Active);

        if (!isOwner && !isLeader)
        {
            throw ErrorHelper.Forbidden(
                "You can only delete your own assignments. Trip leaders can delete any assignment.");
        }

        await _unitOfWork.PackingAssignments.SoftRemoveRangeById(new List<Guid> { assignmentId });
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Packing assignment {AssignmentId} deleted successfully", assignmentId);

        return true;
    }

    public async Task<PackingAssignmentDto> GetAssignmentByIdAsync(Guid assignmentId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        var assignment = await _unitOfWork.PackingAssignments.GetQueryable()
            .Include(pa => pa.PackingItem)
                .ThenInclude(pi => pi.Trip)
            .Include(pa => pa.User)
            .FirstOrDefaultAsync(pa => pa.Id == assignmentId);

        if (assignment == null)
        {
            throw ErrorHelper.NotFound("The packing assignment does not exist.");
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == assignment.PackingItem.Trip.GroupId
                && gm.UserId == currentUserId
                && gm.Status == Domain.Enums.GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to view packing assignments.");
        }

        return await MapToDtoAsync(assignment);
    }

    public async Task<IEnumerable<PackingAssignmentDto>> GetAssignmentsByPackingItemIdAsync(Guid packingItemId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} retrieving assignments for packing item {PackingItemId}",
            currentUserId, packingItemId);

        var packingItem = await _unitOfWork.PackingItems.GetQueryable()
            .Include(pi => pi.Trip)
            .FirstOrDefaultAsync(pi => pi.Id == packingItemId);

        if (packingItem == null)
        {
            throw ErrorHelper.NotFound("The packing item does not exist.");
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == packingItem.Trip.GroupId
                && gm.UserId == currentUserId
                && gm.Status == Domain.Enums.GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to view packing assignments.");
        }

        var assignments = await _unitOfWork.PackingAssignments.GetQueryable()
            .Include(pa => pa.PackingItem)
            .Include(pa => pa.User)
            .Where(pa => pa.PackingItemId == packingItemId)
            .ToListAsync();

        var dtos = new List<PackingAssignmentDto>();
        foreach (var assignment in assignments)
        {
            dtos.Add(await MapToDtoAsync(assignment));
        }

        return dtos;
    }

    public async Task<IEnumerable<PackingAssignmentDto>> GetAssignmentsByUserIdAsync(Guid userId, Guid tripId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} retrieving assignments for user {TargetUserId} in trip {TripId}",
            currentUserId, userId, tripId);

        var trip = await _unitOfWork.Trips.GetByIdAsync(tripId);
        if (trip == null)
        {
            throw ErrorHelper.NotFound("The trip does not exist.");
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == trip.GroupId
                && gm.UserId == currentUserId
                && gm.Status == Domain.Enums.GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to view packing assignments.");
        }

        var assignments = await _unitOfWork.PackingAssignments.GetQueryable()
            .Include(pa => pa.PackingItem)
            .Include(pa => pa.User)
            .Where(pa => pa.UserId == userId && pa.PackingItem.TripId == tripId)
            .ToListAsync();

        var dtos = new List<PackingAssignmentDto>();
        foreach (var assignment in assignments)
        {
            dtos.Add(await MapToDtoAsync(assignment));
        }

        return dtos;
    }

    public async Task<PackingItemAssignmentSummaryDto> GetAssignmentSummaryAsync(Guid packingItemId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} retrieving assignment summary for packing item {PackingItemId}",
            currentUserId, packingItemId);

        var packingItem = await _unitOfWork.PackingItems.GetQueryable()
            .Include(pi => pi.Trip)
            .FirstOrDefaultAsync(pi => pi.Id == packingItemId);

        if (packingItem == null)
        {
            throw ErrorHelper.NotFound("The packing item does not exist.");
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == packingItem.Trip.GroupId
                && gm.UserId == currentUserId
                && gm.Status == Domain.Enums.GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to view packing item summaries.");
        }

        var assignments = await _unitOfWork.PackingAssignments.GetQueryable()
            .Include(pa => pa.User)
            .Where(pa => pa.PackingItemId == packingItemId)
            .ToListAsync();

        var assignmentDtos = new List<PackingAssignmentDto>();
        foreach (var assignment in assignments)
        {
            assignmentDtos.Add(await MapToDtoAsync(assignment));
        }

        var totalAssigned = assignments.Sum(a => a.Quantity);
        var remaining = packingItem.QuantityNeeded - totalAssigned;

        return new PackingItemAssignmentSummaryDto
        {
            PackingItemId = packingItem.Id,
            PackingItemName = packingItem.Name,
            Category = packingItem.Category,
            IsShared = packingItem.IsShared,
            QuantityNeeded = packingItem.QuantityNeeded,
            TotalAssigned = totalAssigned,
            Remaining = remaining,
            IsFullyAssigned = totalAssigned >= packingItem.QuantityNeeded,
            AssignmentCount = assignments.Count,
            Assignments = assignmentDtos
        };
    }

    private async Task<PackingAssignmentDto> MapToDtoAsync(PackingAssignment assignment)
    {
        if (assignment.PackingItem == null)
        {
            assignment.PackingItem = await _unitOfWork.PackingItems.GetByIdAsync(assignment.PackingItemId);
        }

        if (assignment.User == null)
        {
            assignment.User = await _unitOfWork.Users.GetByIdAsync(assignment.UserId);
        }

        return new PackingAssignmentDto
        {
            Id = assignment.Id,
            PackingItemId = assignment.PackingItemId,
            PackingItemName = assignment.PackingItem?.Name ?? string.Empty,
            UserId = assignment.UserId,
            UserName = assignment.User?.Username ?? string.Empty,
            UserAvatarUrl = assignment.User?.AvatarUrl,
            Quantity = assignment.Quantity,
            IsChecked = assignment.IsChecked,
            CreatedAt = assignment.CreatedAt,
            UpdatedAt = assignment.UpdatedAt
        };
    }
}
