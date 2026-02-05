using TripTogether.Application.DTOs.PackingItemDTO;

namespace TripTogether.Application.Interfaces;

public interface IPackingItemService
{
    Task<PackingItemDto> CreatePackingItemAsync(CreatePackingItemDto dto);
    Task<PackingItemDto> UpdatePackingItemAsync(Guid packingItemId, UpdatePackingItemDto dto);
    Task<bool> DeletePackingItemAsync(Guid packingItemId);
    Task<PackingItemDto> GetPackingItemByIdAsync(Guid packingItemId);
    Task<IEnumerable<PackingItemDto>> GetPackingItemsByTripIdAsync(Guid tripId);
}
