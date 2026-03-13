using Microsoft.Extensions.Logging;
using VideoManagerService.Application.DTOs;
using VideoManagerService.Application.Interfaces;
using VideoManagerService.Domain.Entities;
using VideoManagerService.Domain.Exceptions;
using VideoManagerService.Domain.Interfaces.Repositories;
using VideoManagerService.Domain.Interfaces.Services;

namespace VideoManagerService.Application.UseCases;

public class UploadVideoUseCase : IUploadVideoUseCase
{
    private readonly IVideoRepository _videoRepository;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<UploadVideoUseCase> _logger;
    private const long MaxFileSizeBytes = 500 * 1024 * 1024;

    public UploadVideoUseCase(
        IVideoRepository videoRepository,
        IFileStorageService fileStorageService,
        ILogger<UploadVideoUseCase> logger)
    {
        _videoRepository = videoRepository;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    public async Task<VideoUploadResponseDto> ExecuteAsync(VideoUploadRequestDto request)
    {
        try
        {
            _logger.LogInformation(
                "Iniciando upload de vídeo. UserId: {UserId}, FileName: {FileName}",
                request.UserId, request.Video.FileName);

            // 1. Validar tamanho do arquivo
            if (request.Video.Length > MaxFileSizeBytes)
            {
                throw new FileSizeExceededException(MaxFileSizeBytes);
            }

            // 2. Gerar nome único para o arquivo
            var timestamp = DateTime.UtcNow.ToString("yyyyMMdd_HHmmss");
            var extension = Path.GetExtension(request.Video.FileName);
            var storedFileName = $"{timestamp}_{Guid.NewGuid()}{extension}";

            // 3. Criar entidade de domínio
            var videoUpload = new VideoUpload(
                request.UserId,
                request.Video.FileName,
                storedFileName,
                Path.Combine("uploads", storedFileName),
                request.Video.Length
            );

            // 4. Validar formato de vídeo
            if (!videoUpload.HasValidVideoExtension())
            {
                throw new InvalidFileFormatException(request.Video.FileName);
            }

            // 5. Salvar arquivo no storage
            using (var stream = request.Video.OpenReadStream())
            {
                await _fileStorageService.SaveFileAsync(stream, storedFileName, $"uploads/{videoUpload.Id}");
            }

            // 6. Salvar metadados no repositório
            await _videoRepository.SaveAsync(videoUpload);

            _logger.LogInformation(
                "Upload concluído com sucesso. VideoId: {VideoId}",
                videoUpload.Id);

            return new VideoUploadResponseDto
            {
                Success = true,
                Message = "Upload realizado com sucesso. Aguardando processamento pelo video-processor.",
                VideoId = videoUpload.Id
            };
        }
        catch (FileSizeExceededException ex)
        {
            _logger.LogWarning(ex, "Arquivo excede tamanho máximo");
            return new VideoUploadResponseDto
            {
                Success = false,
                Message = ex.Message
            };
        }
        catch (InvalidFileFormatException ex)
        {
            _logger.LogWarning(ex, "Formato de arquivo inválido");
            return new VideoUploadResponseDto
            {
                Success = false,
                Message = ex.Message
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao processar upload de vídeo");
            return new VideoUploadResponseDto
            {
                Success = false,
                Message = "Erro interno ao processar upload. Tente novamente."
            };
        }
    }
}
