namespace TripTogether.Application.DTOs.FriendshipDTO
{
    public class FriendshipDto
    {
        public Guid Id { get; set; }
        public Guid AddresseeId { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public UserBasicInfoDto Requester { get; set; } = null!;
        public UserBasicInfoDto Addressee { get; set; } = null!;
    }
}
