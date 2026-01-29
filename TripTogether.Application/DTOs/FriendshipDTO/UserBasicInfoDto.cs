namespace TripTogether.Application.DTOs.FriendshipDTO
{
    public class UserBasicInfoDto
    {
        public Guid Id { get; set; }
        public string Username { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? AvatarUrl { get; set; }

    }
}
