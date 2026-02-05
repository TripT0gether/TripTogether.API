using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TripTogether.Application.DTOs.PackingItemDTO;
using TripTogether.Application.Interfaces;

namespace TripTogether.Application.Services;

public sealed class PackingItemService : IPackingItemService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly ILogger<PackingItemService> _loggerService;

    public PackingItemService(
        IUnitOfWork unitOfWork,
        IClaimsService claimsService,
        ILogger<PackingItemService> loggerService)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _loggerService = loggerService;
    }

    public async Task<PackingItemDto> CreatePackingItemAsync(CreatePackingItemDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} creating packing item: {Name} for trip {TripId}",
            currentUserId, dto.Name, dto.TripId);

        var trip = await _unitOfWork.Trips.GetByIdAsync(dto.TripId);
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
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to create packing items.");
        }

        var packingItem = new PackingItem
        {
            TripId = dto.TripId,
            Name = dto.Name,
            Category = dto.Category,
            IsShared = dto.IsShared,
            QuantityNeeded = dto.QuantityNeeded
        };

        await _unitOfWork.PackingItems.AddAsync(packingItem);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Packing item {PackingItemId} created successfully", packingItem.Id);

        return MapToDto(packingItem);
    }

    public async Task<PackingItemDto> UpdatePackingItemAsync(Guid packingItemId, UpdatePackingItemDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} updating packing item {PackingItemId}",
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
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to update packing items.");
        }

        if (dto.Name != null) packingItem.Name = dto.Name;
        if (dto.Category != null) packingItem.Category = dto.Category;
        if (dto.IsShared.HasValue) packingItem.IsShared = dto.IsShared.Value;
        if (dto.QuantityNeeded.HasValue) packingItem.QuantityNeeded = dto.QuantityNeeded.Value;

        await _unitOfWork.PackingItems.Update(packingItem);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Packing item {PackingItemId} updated successfully", packingItemId);

        return MapToDto(packingItem);
    }

    public async Task<bool> DeletePackingItemAsync(Guid packingItemId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} deleting packing item {PackingItemId}",
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
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to delete packing items.");
        }

        await _unitOfWork.PackingItems.SoftRemoveRangeById(new List<Guid> { packingItemId });
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Packing item {PackingItemId} deleted successfully", packingItemId);

        return true;
    }

    public async Task<PackingItemDto> GetPackingItemByIdAsync(Guid packingItemId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

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
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to view packing items.");
        }

        return MapToDto(packingItem);
    }

    public async Task<IEnumerable<PackingItemDto>> GetPackingItemsByTripIdAsync(Guid tripId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {UserId} retrieving packing items for trip {TripId}",
            currentUserId, tripId);

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
            throw ErrorHelper.Forbidden("You must be a member of the trip's group to view packing items.");
        }

        var packingItems = await _unitOfWork.PackingItems.GetQueryable()
            .Where(pi => pi.TripId == tripId)
            .OrderBy(pi => pi.Category)
            .ThenBy(pi => pi.Name)
            .ToListAsync();

        return packingItems.Select(MapToDto);
    }

    private PackingItemDto MapToDto(PackingItem packingItem)
    {
        return new PackingItemDto
        {
            Id = packingItem.Id,
            TripId = packingItem.TripId,
            Name = packingItem.Name,
            Category = packingItem.Category,
            IsShared = packingItem.IsShared,
            QuantityNeeded = packingItem.QuantityNeeded,
            CreatedAt = packingItem.CreatedAt,
            UpdatedAt = packingItem.UpdatedAt
        };
    }
}
