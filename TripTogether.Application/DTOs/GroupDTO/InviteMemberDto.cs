using System.ComponentModel.DataAnnotations;

namespace TripTogether.Application.DTOs.GroupDTO;

public class InviteMemberDto
{
    [Required(ErrorMessage = "UserId is required")]
    public Guid UserId { get; set; }
}