using Microsoft.EntityFrameworkCore;
using VideoManagerService.Domain.Entities;
using VideoManagerService.Domain.Enums;
using VideoManagerService.Domain.Interfaces.Repositories;
using VideoManagerService.Infrastructure.Data;

namespace VideoManagerService.Infrastructure.Persistence;

/// <summary>
/// Implementação do repositório de vídeos usando Entity Framework Core e PostgreSQL
/// </summary>
public class EFVideoRepository : IVideoRepository
{
    private readonly VideoManagerDbContext _context;

    public EFVideoRepository(VideoManagerDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public async Task<VideoUpload> SaveAsync(VideoUpload video)
    {
        if (video == null)
            throw new ArgumentNullException(nameof(video));

        await _context.VideoUploads.AddAsync(video);
        await _context.SaveChangesAsync();
        return video;
    }

    public async Task<VideoUpload?> GetByIdAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return null;

        return await _context.VideoUploads
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.Id == id);
    }

    public async Task<IEnumerable<VideoUpload>> GetByUserIdAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
            return Enumerable.Empty<VideoUpload>();

        return await _context.VideoUploads
            .AsNoTracking()
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.UploadedAt)
            .ToListAsync();
    }

    public async Task UpdateAsync(VideoUpload video)
    {
        if (video == null)
            throw new ArgumentNullException(nameof(video));

        _context.VideoUploads.Update(video);
        await _context.SaveChangesAsync();
    }

    public async Task DeleteAsync(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
            return;

        var video = await _context.VideoUploads.FindAsync(id);
        if (video != null)
        {
            _context.VideoUploads.Remove(video);
            await _context.SaveChangesAsync();
        }
    }

    public async Task<IEnumerable<VideoUpload>> GetByStatusAsync(ProcessingStatus status)
    {
        return await _context.VideoUploads
            .AsNoTracking()
            .Where(v => v.Status == status)
            .OrderBy(v => v.UploadedAt)
            .ToListAsync();
    }
}
