using VideoManagerService.Application.DTOs;

namespace VideoManagerService.Application.Interfaces;

/// <summary>
/// Interface para o caso de uso de listagem de vídeos do usuário
/// </summary>
public interface IGetUserVideosUseCase
{
    Task<IEnumerable<VideoInfoDto>> ExecuteAsync(string userId);
}
