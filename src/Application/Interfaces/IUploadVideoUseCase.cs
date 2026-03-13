using VideoManagerService.Application.DTOs;

namespace VideoManagerService.Application.Interfaces;

/// <summary>
/// Interface para o caso de uso de upload de vídeo
/// </summary>
public interface IUploadVideoUseCase
{
    Task<VideoUploadResponseDto> ExecuteAsync(VideoUploadRequestDto request);
}
