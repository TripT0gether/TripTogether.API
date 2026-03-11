using Microsoft.AspNetCore.Http;
using TripTogether.Application.DTOs.UserDTO;

namespace TripTogether.Application.Interfaces;

public interface IAccountService
{
    Task<UserDto?> GetCurrentUserAsync();
    Task<UserDto?> GetUserByIdAsync(Guid userId);
    Task<UserDto?> UpdateUserProfileAsync(UpdateUserDto updateUserDto);
    Task<bool> DeleteAccountAsync();
    Task<string> UploadAvatarAsync(IFormFile file);
}
