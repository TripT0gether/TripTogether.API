using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TripTogether.Application.Interfaces;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Services;

public class FileService : IFileService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly IBlobService _blobService;
    private readonly ILogger<FileService> _loggerService;

    public FileService(
        IUnitOfWork unitOfWork,
        IClaimsService claimsService,
        IBlobService blobService,
        ILogger<FileService> loggerService)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _blobService = blobService;
        _loggerService = loggerService;
    }

    public async Task<string> UploadAvatarAsync(IFormFile file)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} uploading avatar", currentUserId);

        if (file == null || file.Length == 0)
            throw ErrorHelper.BadRequest("No file provided.");

        var user = await _unitOfWork.Users.GetByIdAsync(currentUserId);
        if (user == null)
            throw ErrorHelper.NotFound("User not found.");

        // Delete old avatar if exists
        if (!string.IsNullOrEmpty(user.AvatarUrl))
        {
            _loggerService.LogInformation("Deleting old avatar for user {CurrentUserId}", currentUserId);
            await _blobService.DeleteFileAsync(user.AvatarUrl);
        }

        var fileName = $"{currentUserId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var folder = $"avatars/{currentUserId}";

        using var stream = file.OpenReadStream();
        await _blobService.UploadFileAsync(fileName, stream, folder);

        var avatarUrl = await _blobService.GetFileUrlAsync($"{folder}/{fileName}");
        if (string.IsNullOrEmpty(avatarUrl))
        {
            _loggerService.LogError("Failed to generate URL for avatar");
            throw ErrorHelper.Internal("Could not generate file URL.");
        }

        user.AvatarUrl = avatarUrl;
        await _unitOfWork.Users.Update(user);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Avatar uploaded successfully for user {CurrentUserId}", currentUserId);

        return avatarUrl;
    }

    public async Task<string> UploadGroupCoverPhotoAsync(Guid groupId, IFormFile file)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _loggerService.LogInformation("User {CurrentUserId} uploading cover photo for group {GroupId}", currentUserId, groupId);

        if (file == null || file.Length == 0)
            throw ErrorHelper.BadRequest("No file provided.");

        var group = await _unitOfWork.Groups.GetByIdAsync(groupId);
        if (group == null)
            throw ErrorHelper.NotFound("The group does not exist.");

        var isLeader = await _unitOfWork.GroupMembers.FirstOrDefaultAsync(gm =>
            gm.UserId == currentUserId &&
            gm.GroupId == groupId &&
            gm.Role == GroupMemberRole.Leader &&
            gm.Status == GroupMemberStatus.Active);

        if (isLeader == null)
            throw ErrorHelper.Forbidden("Only the team leader has the right to change the cover photo.");

        // Delete old cover photo if exists
        if (!string.IsNullOrEmpty(group.CoverPhotoUrl))
        {
            _loggerService.LogInformation("Deleting old cover photo for group {GroupId}", groupId);
            await _blobService.DeleteFileAsync(group.CoverPhotoUrl);
        }

        var fileName = $"{groupId}_{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var folder = "group-covers";

        using var stream = file.OpenReadStream();
        await _blobService.UploadFileAsync(fileName, stream, folder);

        var coverPhotoUrl = await _blobService.GetFileUrlAsync($"{folder}/{fileName}");
        if (string.IsNullOrEmpty(coverPhotoUrl))
        {
            _loggerService.LogError("Failed to generate URL for cover photo");
            throw ErrorHelper.Internal("Could not generate file URL.");
        }

        group.CoverPhotoUrl = coverPhotoUrl;
        await _unitOfWork.Groups.Update(group);
        await _unitOfWork.SaveChangesAsync();

        _loggerService.LogInformation("Cover photo uploaded successfully for group {GroupId}", groupId);

        return coverPhotoUrl;
    }
}
