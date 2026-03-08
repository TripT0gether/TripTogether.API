using Microsoft.AspNetCore.Http;
using TripTogether.Application.DTOs.GalleryDTO;

namespace TripTogether.Application.Interfaces;

public interface IGalleryService
{
    Task<GalleryDto> CreateGalleryAsync(CreateGalleryDto dto, IFormFile file);
    Task<GalleryDto> UpdateGalleryAsync(Guid galleryId, UpdateGalleryDto dto);
    Task<bool> DeleteGalleryAsync(Guid galleryId);
    Task<GalleryDto> GetGalleryByIdAsync(Guid galleryId);
    Task<IEnumerable<GalleryDto>> GetAllGalleriesAsync(GalleryQueryDto query);
}
