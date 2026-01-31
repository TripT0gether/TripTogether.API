using TripTogether.Application.DTOs.TripDTO;
using TripTogether.Application.Utils;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Interfaces;

public interface ITripService
{
    Task<TripDto> CreateTripAsync(CreateTripDto dto);
    Task<TripDto> UpdateTripAsync(Guid tripId, UpdateTripDto dto);
    Task<bool> DeleteTripAsync(Guid tripId);
    Task<TripDetailDto> GetTripDetailAsync(Guid tripId);
    Task<TripDto> GetTripByTokenAsync(string token);
    Task<Pagination<TripDto>> GetGroupTripsAsync(Guid groupId, TripQueryDto query);
    Task<TripDto> UpdateTripStatusAsync(Guid tripId, TripStatus status);
    Task<Pagination<TripDto>> GetMyTripsAsync(TripQueryDto query);
}
