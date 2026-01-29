using System.ComponentModel.DataAnnotations;

namespace TripTogether.Application.DTOs.FriendshipDTO
{
    public class SendFriendRequestDto
    {
        [Required(ErrorMessage = "AddresseeId is required")]
        public Guid AddresseeId { get; set; }
    }
}
