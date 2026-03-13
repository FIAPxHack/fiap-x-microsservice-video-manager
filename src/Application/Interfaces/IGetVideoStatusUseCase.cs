using VideoManagerService.Application.DTOs;

namespace VideoManagerService.Application.Interfaces;

/// <summary>
/// Interface para o caso de uso de consulta de status de um vídeo
/// </summary>
public interface IGetVideoStatusUseCase
{
    Task<VideoInfoDto?> ExecuteAsync(string videoId);
}
