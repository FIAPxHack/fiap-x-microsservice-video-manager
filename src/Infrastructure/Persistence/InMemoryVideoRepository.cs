using System.Collections.Concurrent;
using VideoManagerService.Domain.Entities;
using VideoManagerService.Domain.Enums;
using VideoManagerService.Domain.Interfaces.Repositories;

namespace VideoManagerService.Infrastructure.Persistence;

/// <summary>
/// Implementação em memória do repositório de vídeos para desenvolvimento e testes.
/// </summary>
public class InMemoryVideoRepository : IVideoRepository
{
    private readonly ConcurrentDictionary<string, VideoUpload> _videos = new();

    public Task<VideoUpload> SaveAsync(VideoUpload video)
    {
        if (video == null)
            throw new ArgumentNullException(nameof(video));

        _videos.TryAdd(video.Id, video);
        return Task.FromResult(video);
    }

    public Task<VideoUpload?> GetByIdAsync(string id)
    {
        _videos.TryGetValue(id, out var video);
        return Task.FromResult(video);
    }

    public Task<IEnumerable<VideoUpload>> GetByUserIdAsync(string userId)
    {
        var videos = _videos.Values
            .Where(v => v.UserId == userId)
            .OrderByDescending(v => v.UploadedAt)
            .AsEnumerable();

        return Task.FromResult(videos);
    }

    public Task UpdateAsync(VideoUpload video)
    {
        if (video == null)
            throw new ArgumentNullException(nameof(video));

        _videos[video.Id] = video;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(string id)
    {
        _videos.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task<IEnumerable<VideoUpload>> GetByStatusAsync(ProcessingStatus status)
    {
        var videos = _videos.Values
            .Where(v => v.Status == status)
            .OrderByDescending(v => v.UploadedAt)
            .AsEnumerable();

        return Task.FromResult(videos);
    }
}
