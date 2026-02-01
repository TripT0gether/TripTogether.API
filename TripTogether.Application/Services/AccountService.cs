using Microsoft.Extensions.Logging;
using TripTogether.Application.DTOs.UserDTO;
using TripTogether.Application.Interfaces;

namespace TripTogether.Application.Services;

public class AccountService : IAccountService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger _loggerService;
    private readonly IClaimsService _claimsService;

    public AccountService(IUnitOfWork unitOfWork, ILogger<AccountService> logger, IClaimsService claimsService)
    {
        _unitOfWork = unitOfWork;
        _loggerService = logger;
        _claimsService = claimsService;
    }

    /// <summary>
    ///     Get current authenticated user profile.
    /// </summary>
    /// <returns></returns>
    public async Task<UserDto?> GetCurrentUserAsync()
    {
        var userId = _claimsService.GetCurrentUserId;
        _loggerService.LogInformation("Getting profile for user ID: {UserId}", userId);

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
        {
            _loggerService.LogWarning($"User with ID {userId} not found");
            throw ErrorHelper.NotFound("Account does not exist.");
        }

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Gender = user.Gender,
            PaymentQrCodeUrl = user.PaymentQrCodeUrl,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>
    ///     Get user profile by user ID.
    /// </summary>
    /// <param name="userId"></param>
    /// <returns></returns>
    public async Task<UserDto?> GetUserByIdAsync(Guid userId)
    {
        _loggerService.LogInformation("Getting profile for user ID: {UserId}", userId);

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
        {
            _loggerService.LogWarning("User with ID {UserId} not found", userId);
            throw ErrorHelper.NotFound("Account does not exist.");
        }

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Gender = user.Gender,
            PaymentQrCodeUrl = user.PaymentQrCodeUrl,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>
    ///     Update current user profile.
    /// </summary>
    /// <param name="updateUserDto"></param>
    /// <returns></returns>
    public async Task<UserDto?> UpdateUserProfileAsync(UpdateUserDto updateUserDto)
    {
        var userId = _claimsService.GetCurrentUserId;
        _loggerService.LogInformation("Updating profile for user ID: {UserId}", userId);

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
        {
            _loggerService.LogWarning($"User with ID {userId} not found");
            throw ErrorHelper.NotFound("Account does not exist.");
        }

        if (user.IsDeleted)
            throw ErrorHelper.Forbidden("Account has been disabled or banned.");

        // Update only provided fields
        if (!string.IsNullOrWhiteSpace(updateUserDto.Username))
        {
            // Check if username is already taken
            var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u =>
                u.Username == updateUserDto.Username && u.Id != userId && !u.IsDeleted);

            if (existingUser != null)
            {
                _loggerService.LogWarning("Username {Username} is already taken", updateUserDto.Username);
                throw ErrorHelper.Conflict("Username is already taken.");
            }

            user.Username = updateUserDto.Username;
        }

        if (!string.IsNullOrWhiteSpace(updateUserDto.Email))
        {
            // Check if email is already taken
            var existingUser = await _unitOfWork.Users.FirstOrDefaultAsync(u =>
                u.Email == updateUserDto.Email && u.Id != userId && !u.IsDeleted);

            if (existingUser != null)
            {
                _loggerService.LogWarning("Email {Email} is already taken", updateUserDto.Email);
                throw ErrorHelper.Conflict("Email is already taken.");
            }

            user.Email = updateUserDto.Email;
        }

        if (updateUserDto.Gender.HasValue)
            user.Gender = updateUserDto.Gender.Value;

        if (updateUserDto.PaymentQrCodeUrl != null)
            user.PaymentQrCodeUrl = updateUserDto.PaymentQrCodeUrl;

        await _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Profile updated successfully for user ID: {UserId}", userId);

        return new UserDto
        {
            Id = user.Id,
            Username = user.Username,
            Email = user.Email,
            AvatarUrl = user.AvatarUrl,
            Gender = user.Gender,
            PaymentQrCodeUrl = user.PaymentQrCodeUrl,
            IsEmailVerified = user.IsEmailVerified,
            CreatedAt = user.CreatedAt
        };
    }

    /// <summary>
    ///     Soft delete current user account.
    /// </summary>
    /// <returns></returns>
    public async Task<bool> DeleteAccountAsync()
    {
        var userId = _claimsService.GetCurrentUserId;
        _loggerService.LogInformation("Account deletion requested for user ID: {UserId}", userId);

        var user = await _unitOfWork.Users.FirstOrDefaultAsync(u => u.Id == userId && !u.IsDeleted);

        if (user == null)
        {
            _loggerService.LogWarning($"User with ID {userId} not found");
            throw ErrorHelper.NotFound("Account does not exist.");
        }

        if (user.IsDeleted)
            throw ErrorHelper.Forbidden("Account has already been deleted.");

        // Soft delete
        await _unitOfWork.Users.SoftRemove(user);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Account deleted successfully for user ID: {UserId}", userId);
        return true;
    }
}
