using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TripTogether.Application.DTOs.GalleryDTO;
using TripTogether.Application.Interfaces;
using TripTogether.Domain.Enums;

namespace TripTogether.Application.Services;

public sealed class GalleryService : IGalleryService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IClaimsService _claimsService;
    private readonly IBlobService _blobService;
    private readonly ILogger<GalleryService> _logger;

    public GalleryService(
        IUnitOfWork unitOfWork,
        IClaimsService claimsService,
        IBlobService blobService,
        ILogger<GalleryService> logger)
    {
        _unitOfWork = unitOfWork;
        _claimsService = claimsService;
        _blobService = blobService;
        _logger = logger;
    }

    public async Task<GalleryDto> CreateGalleryAsync(CreateGalleryDto dto, IFormFile file)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _logger.LogInformation("User {UserId} creating gallery image", currentUserId);

        if (file == null || file.Length == 0)
        {
            throw ErrorHelper.BadRequest("No image file provided.");
        }

        var trip = await _unitOfWork.Trips.GetByIdAsync(dto.TripId);
        if (trip == null)
        {
            throw ErrorHelper.NotFound("The trip does not exist.");
        }

        var groupId = trip.GroupId;

        if (dto.ActivityId.HasValue)
        {
            var activity = await _unitOfWork.Activities
                .GetQueryable()
                .Include(a => a.Trip)
                .FirstOrDefaultAsync(a => a.Id == dto.ActivityId.Value);

            if (activity == null)
            {
                throw ErrorHelper.NotFound("The activity does not exist.");
            }

            if (activity.TripId != dto.TripId)
            {
                throw ErrorHelper.BadRequest("The activity does not belong to the specified trip.");
            }
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == groupId
                && gm.UserId == currentUserId
                && gm.Status == GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to add gallery images.");
        }

        // Upload image to storage
        var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
        var folder = dto.ActivityId.HasValue
            ? $"galleries/{dto.TripId}/activities/{dto.ActivityId}"
            : $"galleries/{dto.TripId}";

        using var stream = file.OpenReadStream();
        await _blobService.UploadFileAsync(fileName, stream, folder);

        var imageUrl = await _blobService.GetFileUrlAsync($"{folder}/{fileName}");
        if (string.IsNullOrEmpty(imageUrl))
        {
            _logger.LogError("Failed to generate URL for gallery image");
            throw ErrorHelper.Internal("Could not generate file URL.");
        }

        var gallery = new Gallery
        {
            TripId = dto.TripId,
            ActivityId = dto.ActivityId,
            ImageUrl = imageUrl,
            Caption = dto.Caption
        };

        await _unitOfWork.Galleries.AddAsync(gallery);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Gallery image {GalleryId} created successfully", gallery.Id);

        return MapToDto(gallery);
    }


    public async Task<GalleryDto> UpdateGalleryAsync(Guid galleryId, UpdateGalleryDto dto)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _logger.LogInformation("User {UserId} updating gallery {GalleryId}", currentUserId, galleryId);

        var gallery = await _unitOfWork.Galleries
            .GetQueryable()
            .Include(g => g.Trip)
            .Include(g => g.Activity)
                .ThenInclude(a => a.Trip)
            .FirstOrDefaultAsync(g => g.Id == galleryId);

        if (gallery == null)
        {
            throw ErrorHelper.NotFound("Gallery image not found.");
        }

        Guid groupId;
        if (gallery.Activity != null)
        {
            groupId = gallery.Activity.Trip.GroupId;
        }
        else if (gallery.Trip != null)
        {
            groupId = gallery.Trip.GroupId;
        }
        else
        {
            throw ErrorHelper.BadRequest("Gallery must be associated with either a Trip or Activity.");
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == groupId
                && gm.UserId == currentUserId
                && gm.Status == GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to update gallery images.");
        }

        if (dto.Caption != null)
        {
            gallery.Caption = dto.Caption;
        }

        await _unitOfWork.Galleries.Update(gallery);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Gallery {GalleryId} updated successfully", galleryId);

        return MapToDto(gallery);
    }

    public async Task<bool> DeleteGalleryAsync(Guid galleryId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _logger.LogInformation("User {UserId} deleting gallery {GalleryId}", currentUserId, galleryId);

        var gallery = await _unitOfWork.Galleries
            .GetQueryable()
            .Include(g => g.Trip)
            .Include(g => g.Activity)
                .ThenInclude(a => a.Trip)
            .FirstOrDefaultAsync(g => g.Id == galleryId);

        if (gallery == null)
        {
            throw ErrorHelper.NotFound("Gallery image not found.");
        }

        Guid groupId;
        if (gallery.Activity != null)
        {
            groupId = gallery.Activity.Trip.GroupId;
        }
        else if (gallery.Trip != null)
        {
            groupId = gallery.Trip.GroupId;
        }
        else
        {
            throw ErrorHelper.BadRequest("Gallery must be associated with either a Trip or Activity.");
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == groupId
                && gm.UserId == currentUserId
                && gm.Status == GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to delete gallery images.");
        }

        // Delete image from storage
        if (!string.IsNullOrEmpty(gallery.ImageUrl))
        {
            _logger.LogInformation("Deleting image from storage for gallery {GalleryId}", galleryId);
            await _blobService.DeleteFileAsync(gallery.ImageUrl);
        }

        await _unitOfWork.Galleries.SoftRemove(gallery);
        await _unitOfWork.SaveChangesAsync();

        _logger.LogInformation("Gallery {GalleryId} deleted successfully", galleryId);

        return true;
    }

    public async Task<GalleryDto> GetGalleryByIdAsync(Guid galleryId)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        var gallery = await _unitOfWork.Galleries
            .GetQueryable()
            .Include(g => g.Trip)
            .Include(g => g.Activity)
                .ThenInclude(a => a.Trip)
            .FirstOrDefaultAsync(g => g.Id == galleryId && !g.IsDeleted);

        if (gallery == null)
        {
            throw ErrorHelper.NotFound("Gallery image not found.");
        }

        Guid groupId;
        if (gallery.Activity != null)
        {
            groupId = gallery.Activity.Trip.GroupId;
        }
        else if (gallery.Trip != null)
        {
            groupId = gallery.Trip.GroupId;
        }
        else
        {
            throw ErrorHelper.BadRequest("Gallery must be associated with either a Trip or Activity.");
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == groupId
                && gm.UserId == currentUserId
                && gm.Status == GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to view gallery images.");
        }

        return MapToDto(gallery);
    }

    public async Task<IEnumerable<GalleryDto>> GetAllGalleriesAsync(GalleryQueryDto query)
    {
        var currentUserId = _claimsService.GetCurrentUserId;

        _logger.LogInformation("User {UserId} retrieving all galleries with query", currentUserId);

        var trip = await _unitOfWork.Trips.GetByIdAsync(query.TripId);

        if (trip == null)
        {
            throw ErrorHelper.NotFound("The trip does not exist.");
        }

        var isMember = await _unitOfWork.GroupMembers.GetQueryable()
            .AnyAsync(gm => gm.GroupId == trip.GroupId
                && gm.UserId == currentUserId
                && gm.Status == GroupMemberStatus.Active);

        if (!isMember)
        {
            throw ErrorHelper.Forbidden("You must be a member of the group to view gallery images.");
        }

        var queryable = _unitOfWork.Galleries
            .GetQueryable()
            .Include(g => g.Trip)
            .Include(g => g.Activity)
            .ThenInclude(a => a.Trip)
            .Where(g => !g.IsDeleted)
            .AsQueryable();

        queryable = queryable.Where(g => g.TripId == query.TripId);

        if (query.ActivityId.HasValue)
        {
            var activityExists = await _unitOfWork.Activities
                .GetQueryable()
                .AnyAsync(a => a.Id == query.ActivityId.Value && a.TripId == query.TripId);

            if (!activityExists)
            {
                throw ErrorHelper.NotFound("The specified activity does not exist in the trip.");
            }

            queryable = queryable.Where(g => g.ActivityId == query.ActivityId.Value);
        }

        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            var searchTerm = query.SearchTerm.ToLower();
            queryable = queryable.Where(g =>
                (g.Caption != null && g.Caption.ToLower().Contains(searchTerm)));
        }

        queryable = queryable
            .OrderByDescending(g => g.CreatedAt);

        var galleries = await queryable.ToListAsync();

        return galleries.Select(MapToDto);
    }

    private static GalleryDto MapToDto(Gallery gallery)
    {
        return new GalleryDto
        {
            Id = gallery.Id,
            TripId = gallery.TripId,
            ActivityId = gallery.ActivityId,
            ImageUrl = gallery.ImageUrl,
            Caption = gallery.Caption,
            CreatedAt = gallery.CreatedAt,
            CreatedBy = gallery.CreatedBy
        };
    }
}
