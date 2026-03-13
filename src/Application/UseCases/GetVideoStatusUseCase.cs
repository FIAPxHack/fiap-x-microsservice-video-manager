using Microsoft.Extensions.Logging;
using VideoManagerService.Application.DTOs;
using VideoManagerService.Application.Interfaces;
using VideoManagerService.Domain.Interfaces.Repositories;

namespace VideoManagerService.Application.UseCases;

/// <summary>
/// Caso de uso: Buscar status de um vídeo específico
/// </summary>
public class GetVideoStatusUseCase : IGetVideoStatusUseCase
{
    private readonly IVideoRepository _videoRepository;
    private readonly ILogger<GetVideoStatusUseCase> _logger;

    public GetVideoStatusUseCase(
        IVideoRepository videoRepository,
        ILogger<GetVideoStatusUseCase> logger)
    {
        _videoRepository = videoRepository;
        _logger = logger;
    }

    public async Task<VideoInfoDto?> ExecuteAsync(string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            _logger.LogWarning("Tentativa de buscar vídeo com videoId vazio");
            return null;
        }

        _logger.LogInformation("Buscando status do vídeo {VideoId}", videoId);

        var video = await _videoRepository.GetByIdAsync(videoId);

        if (video == null)
        {
            _logger.LogWarning("Vídeo {VideoId} não encontrado", videoId);
            return null;
        }

        return new VideoInfoDto
        {
            Id = video.Id,
            UserId = video.UserId,
            OriginalFileName = video.OriginalFileName,
            FileSizeBytes = video.FileSizeBytes,
            Status = video.Status,
            UploadedAt = video.UploadedAt,
            ProcessingStartedAt = video.ProcessingStartedAt,
            ProcessingCompletedAt = video.ProcessingCompletedAt,
            ZipFileName = video.ZipFileName,
            FrameCount = video.FrameCount,
            ErrorMessage = video.ErrorMessage,
            ProcessingDuration = video.GetProcessingDuration()?.ToString(@"hh\:mm\:ss")
        };
    }
}
