namespace TripTogether.Application.DTOs.GroupInviteDTO;

public class GroupInviteDto
{
    public Guid Id { get; set; }
    public Guid GroupId { get; set; }
    public string GroupName { get; set; } = null!;
    public string Token { get; set; } = null!;
    public DateTime ExpiresAt { get; set; }
    public bool IsExpired { get; set; }
    public DateTime CreatedAt { get; set; }
}
