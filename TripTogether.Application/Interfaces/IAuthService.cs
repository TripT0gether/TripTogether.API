using Microsoft.Extensions.Configuration;
using TripTogether.Application.DTOs.AuthDTO;
using TripTogether.Application.DTOs.UserDTO;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Interfaces
{
    public interface IAuthService
    {
        Task<UserDto?> RegisterUserAsync(UserRegistrationDto registrationDto);

        Task<LoginResponseDto?> LoginAsync(LoginRequestDto loginDto, IConfiguration configuration);

        Task<bool> LogoutAsync();

        Task<LoginResponseDto?> RefreshTokenAsync(TokenRefreshRequestDto refreshTokenDto, IConfiguration configuration);

        Task<bool> VerifyEmailOtpAsync(string email, string otp);

        Task<bool> ResendOtpAsync(string email, OtpPurpose otpPurpose);

        Task<bool> ResetPasswordAsync(string email, string otp, string newPassword);
    }
}