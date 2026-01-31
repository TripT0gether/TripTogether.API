using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.TripDTO;

public class TripQueryDto
{
    public string? SearchTerm { get; set; }
    public TripStatus? Status { get; set; }
    public TripSortBy SortBy { get; set; } = TripSortBy.CreatedAt;
    public bool SortDescending { get; set; } = true;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 10;
}

public enum TripSortBy
{
    CreatedAt,
    StartDate,
    PlanningRangeStart
}
