using System.ComponentModel.DataAnnotations;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.DTOs.AuthDTO;

public class ResendOtpRequestDto
{
    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = null!;

    [Required(ErrorMessage = "Purpose is required")]
    public OtpPurpose Purpose { get; set; }
}
