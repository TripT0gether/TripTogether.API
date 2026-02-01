using Microsoft.AspNetCore.Http;

namespace TripTogether.Application.Interfaces;

public interface IFileService
{
    Task<string> UploadAvatarAsync(IFormFile file);
    Task<string> UploadGroupCoverPhotoAsync(Guid groupId, IFormFile file);
}
