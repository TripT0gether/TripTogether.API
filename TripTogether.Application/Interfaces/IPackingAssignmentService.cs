using TripTogether.Application.DTOs.PackingAssignmentDTO;

namespace TripTogether.Application.Interfaces;

public interface IPackingAssignmentService
{
    Task<PackingAssignmentDto> CreateAssignmentAsync(CreatePackingAssignmentDto dto);
    Task<PackingAssignmentDto> UpdateAssignmentAsync(Guid assignmentId, UpdatePackingAssignmentDto dto);
    Task<bool> DeleteAssignmentAsync(Guid assignmentId);
    Task<PackingAssignmentDto> GetAssignmentByIdAsync(Guid assignmentId);
    Task<IEnumerable<PackingAssignmentDto>> GetAssignmentsByPackingItemIdAsync(Guid packingItemId);
    Task<IEnumerable<PackingAssignmentDto>> GetAssignmentsByUserIdAsync(Guid userId, Guid tripId);
    Task<PackingItemAssignmentSummaryDto> GetAssignmentSummaryAsync(Guid packingItemId);
}
