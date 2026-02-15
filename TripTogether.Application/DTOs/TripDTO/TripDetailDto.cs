using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.TripDTO;

public class TripDetailDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = null!;
    public string Title { get; set; } = null!;
    public TripStatus Status { get; set; }
    public DateOnly? PlanningRangeStart { get; set; }
    public DateOnly? PlanningRangeEnd { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public TripSettings? Settings { get; set; }
    public DateTime CreatedAt { get; set; }
    public int PollCount { get; set; }
    public int ActivityCount { get; set; }
    public int ExpenseCount { get; set; }
}
