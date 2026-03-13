using VideoManagerService.Application.DTOs;
using VideoManagerService.Application.Interfaces;
using VideoManagerService.Domain.Enums;
using VideoManagerService.Domain.Interfaces.Repositories;
using VideoManagerService.Domain.Interfaces.Services;

namespace VideoManagerService.Application.UseCases;

public class UpdateVideoStatusUseCase : IUpdateVideoStatusUseCase
{
    private readonly IVideoRepository _videoRepository;
    private readonly INotificationServiceClient _notificationClient;
    private readonly ILogger<UpdateVideoStatusUseCase> _logger;

    public UpdateVideoStatusUseCase(
        IVideoRepository videoRepository,
        INotificationServiceClient notificationClient,
        ILogger<UpdateVideoStatusUseCase> logger)
    {
        _videoRepository = videoRepository;
        _notificationClient = notificationClient;
        _logger = logger;
    }

    public async Task<VideoInfoDto?> ExecuteAsync(string videoId, UpdateVideoStatusRequestDto request)
    {
        _logger.LogInformation(
            "Atualizando status do vídeo {VideoId} para {Status}",
            videoId, request.Status);

        var video = await _videoRepository.GetByIdAsync(videoId);
        if (video == null)
        {
            _logger.LogWarning("Vídeo {VideoId} não encontrado", videoId);
            return null;
        }

        switch (request.Status)
        {
            case ProcessingStatus.Processing:
                video.StartProcessing();
                break;

            case ProcessingStatus.Completed:
                if (string.IsNullOrWhiteSpace(request.ZipFileName))
                {
                    _logger.LogWarning("ZipFileName é obrigatório para status Completed");
                    return null;
                }
                video.CompleteProcessing(request.ZipFileName, request.FrameCount);
                _logger.LogInformation(
                    "Processamento concluído: VideoId={VideoId}, Frames={FrameCount}",
                    videoId, request.FrameCount);

                try
                {
                    await _notificationClient.NotifyProcessingCompletedAsync(
                        video.UserId,
                        "user@example.com",
                        video.OriginalFileName,
                        request.FrameCount);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao enviar notificação de conclusão");
                }
                break;

            case ProcessingStatus.Failed:
                video.MarkAsFailed(request.ErrorMessage ?? "Erro no processamento");
                _logger.LogError(
                    "Processamento falhou: VideoId={VideoId}, Erro={ErrorMessage}",
                    videoId, request.ErrorMessage);

                try
                {
                    await _notificationClient.NotifyProcessingFailedAsync(
                        video.UserId,
                        "user@example.com",
                        video.OriginalFileName,
                        request.ErrorMessage ?? "Erro desconhecido");
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Erro ao enviar notificação de falha");
                }
                break;

            default:
                _logger.LogWarning("Status {Status} não suportado para atualização", request.Status);
                return null;
        }

        await _videoRepository.UpdateAsync(video);

        return new VideoInfoDto
        {
            Id = video.Id,
            UserId = video.UserId,
            OriginalFileName = video.OriginalFileName,
            FileSizeBytes = video.FileSizeBytes,
            Status = video.Status,
            FrameCount = video.FrameCount,
            ZipFileName = video.ZipFileName,
            UploadedAt = video.UploadedAt,
            ProcessingStartedAt = video.ProcessingStartedAt,
            ProcessingCompletedAt = video.ProcessingCompletedAt,
            ErrorMessage = video.ErrorMessage
        };
    }
}
