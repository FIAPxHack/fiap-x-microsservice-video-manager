using VideoManagerService.Application.DTOs;

namespace VideoManagerService.Application.Interfaces;

public interface IUpdateVideoStatusUseCase
{
    Task<VideoInfoDto?> ExecuteAsync(string videoId, UpdateVideoStatusRequestDto request);
}
