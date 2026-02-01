using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Swashbuckle.AspNetCore.Annotations;
using TripTogether.Application.DTOs.UserDTO;
using TripTogether.Application.Interfaces;

namespace TripTogether.API.Controllers;

[Route("api/account")]
[ApiController]
[Authorize]
public class AccountController : ControllerBase
{
    private readonly IAccountService _accountService;
    private readonly IFileService _fileService;

    public AccountController(IAccountService accountService, IFileService fileService)
    {
        _accountService = accountService;
        _fileService = fileService;
    }

    /// <summary>
    /// Get current authenticated user profile.
    /// </summary>
    /// <returns>User profile information.</returns>
    [HttpGet("me")]
    [SwaggerOperation(
        Summary = "Get current user profile",
        Description = "Retrieves the profile information of the currently authenticated user."
    )]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 401)]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 404)]
    public async Task<IActionResult> GetCurrentUser()
    {
        var result = await _accountService.GetCurrentUserAsync();
        return Ok(ApiResult<UserDto>.Success(result!, "200", "User profile retrieved successfully."));
    }

    /// <summary>
    /// Get user profile by user ID.
    /// </summary>
    /// <param name="userId">User ID.</param>
    /// <returns>User profile information.</returns>
    [HttpGet("{userId:guid}")]
    [SwaggerOperation(
        Summary = "Get user profile by ID",
        Description = "Retrieves the profile information of a specific user by their ID."
    )]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 404)]
    public async Task<IActionResult> GetUserById([FromRoute] Guid userId)
    {
        var result = await _accountService.GetUserByIdAsync(userId);
        return Ok(ApiResult<UserDto>.Success(result!, "200", "User profile retrieved successfully."));
    }

    /// <summary>
    /// Update current user profile.
    /// </summary>
    /// <param name="updateUserDto">User profile update data.</param>
    /// <returns>Updated user profile information.</returns>
    [HttpPut("me")]
    [SwaggerOperation(
        Summary = "Update user profile",
        Description = "Updates the profile information of the currently authenticated user."
    )]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 200)]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 400)]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 401)]
    [ProducesResponseType(typeof(ApiResult<UserDto>), 404)]
    public async Task<IActionResult> UpdateUserProfile([FromBody] UpdateUserDto updateUserDto)
    {
        var result = await _accountService.UpdateUserProfileAsync(updateUserDto);
        return Ok(ApiResult<UserDto>.Success(result!, "200", "Profile updated successfully."));
    }

    /// <summary>
    /// Delete current user account.
    /// </summary>
    /// <returns>Account deletion result.</returns>
    [HttpDelete("me")]
    [SwaggerOperation(
        Summary = "Delete account",
        Description = "Soft deletes the currently authenticated user account."
    )]
    [ProducesResponseType(typeof(ApiResult<object>), 200)]
    [ProducesResponseType(typeof(ApiResult<object>), 401)]
    [ProducesResponseType(typeof(ApiResult<object>), 404)]
    public async Task<IActionResult> DeleteAccount()
    {
        var result = await _accountService.DeleteAccountAsync();
        return Ok(ApiResult<object>.Success(result, "200", "Account deleted successfully."));
    }

    /// <summary>
    /// Upload user avatar.
    /// </summary>
    /// <param name="file">Avatar image file to upload.</param>
    /// <returns>URL of the uploaded avatar.</returns>
    [HttpPost("me/avatar")]
    [Consumes("multipart/form-data")]
    [SwaggerOperation(
        Summary = "Upload user avatar",
        Description = "Upload an avatar image for the currently authenticated user."
    )]
    [ProducesResponseType(typeof(ApiResult<string>), 200)]
    [ProducesResponseType(typeof(ApiResult<string>), 400)]
    [ProducesResponseType(typeof(ApiResult<string>), 404)]
    [ProducesResponseType(typeof(ApiResult<string>), 500)]
    public async Task<IActionResult> UploadAvatar(IFormFile file)
    {
        var avatarUrl = await _fileService.UploadAvatarAsync(file);
        return Ok(ApiResult<string>.Success(avatarUrl, "200", "Avatar uploaded successfully."));
    }
}
