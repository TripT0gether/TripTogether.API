using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.AnnouncementDTO;

public class AnnouncementQueryDto
{
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public AnnouncementType? Type { get; set; }
    public bool? IsRead { get; set; }
    public Guid? GroupId { get; set; }
    public Guid? TripId { get; set; }
}
