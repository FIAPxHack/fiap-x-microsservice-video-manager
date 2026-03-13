using Microsoft.Extensions.Logging;
using VideoManagerService.Application.DTOs;
using VideoManagerService.Application.Interfaces;
using VideoManagerService.Domain.Interfaces.Repositories;

namespace VideoManagerService.Application.UseCases;

/// <summary>
/// Caso de uso: Buscar vídeos de um usuário
/// </summary>
public class GetUserVideosUseCase : IGetUserVideosUseCase
{
    private readonly IVideoRepository _videoRepository;
    private readonly ILogger<GetUserVideosUseCase> _logger;

    public GetUserVideosUseCase(
        IVideoRepository videoRepository,
        ILogger<GetUserVideosUseCase> logger)
    {
        _videoRepository = videoRepository;
        _logger = logger;
    }

    public async Task<IEnumerable<VideoInfoDto>> ExecuteAsync(string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            _logger.LogWarning("Tentativa de buscar vídeos com userId vazio");
            return Enumerable.Empty<VideoInfoDto>();
        }

        _logger.LogInformation("Buscando vídeos para o usuário {UserId}", userId);

        var videos = await _videoRepository.GetByUserIdAsync(userId);

        var videoDtos = videos
            .OrderByDescending(v => v.UploadedAt)
            .Select(v => new VideoInfoDto
            {
                Id = v.Id,
                UserId = v.UserId,
                OriginalFileName = v.OriginalFileName,
                FileSizeBytes = v.FileSizeBytes,
                Status = v.Status,
                UploadedAt = v.UploadedAt,
                ProcessingStartedAt = v.ProcessingStartedAt,
                ProcessingCompletedAt = v.ProcessingCompletedAt,
                ZipFileName = v.ZipFileName,
                FrameCount = v.FrameCount,
                ErrorMessage = v.ErrorMessage,
                ProcessingDuration = v.GetProcessingDuration()?.ToString(@"hh\:mm\:ss")
            });

        return videoDtos;
    }
}
