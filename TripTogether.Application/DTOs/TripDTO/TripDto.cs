using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.TripDTO;

public class TripDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; }
    public string Title { get; set; } = null!;
    public TripStatus Status { get; set; }
    public DateOnly? PlanningRangeStart { get; set; }
    public DateOnly? PlanningRangeEnd { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public decimal? Budget { get; set; }
    public DateTime CreatedAt { get; set; }
    public string InviteToken { get; set; }
}
