namespace TripTogether.Application.DTOs.GroupDTO;

public class GroupDetailDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? CoverPhotoUrl { get; set; }
    public Guid CreatedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public List<GroupMemberDto> Members { get; set; } = new();
}
