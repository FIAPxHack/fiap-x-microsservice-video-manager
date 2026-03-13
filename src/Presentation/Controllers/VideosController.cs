using Microsoft.AspNetCore.Mvc;
using VideoManagerService.Application.DTOs;
using VideoManagerService.Application.Interfaces;
using VideoManagerService.Domain.Interfaces.Services;

namespace VideoManagerService.Presentation.Controllers;

/// <summary>
/// Controller para gerenciamento de ações do usuário relacionadas a vídeos
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class VideosController : ControllerBase
{
    private readonly IUploadVideoUseCase _uploadVideoUseCase;
    private readonly IGetUserVideosUseCase _getUserVideosUseCase;
    private readonly IGetVideoStatusUseCase _getVideoStatusUseCase;
    private readonly IUpdateVideoStatusUseCase _updateVideoStatusUseCase;
    private readonly IFileStorageService _fileStorageService;
    private readonly ILogger<VideosController> _logger;

    public VideosController(
        IUploadVideoUseCase uploadVideoUseCase,
        IGetUserVideosUseCase getUserVideosUseCase,
        IGetVideoStatusUseCase getVideoStatusUseCase,
        IUpdateVideoStatusUseCase updateVideoStatusUseCase,
        IFileStorageService fileStorageService,
        ILogger<VideosController> logger)
    {
        _uploadVideoUseCase = uploadVideoUseCase;
        _getUserVideosUseCase = getUserVideosUseCase;
        _getVideoStatusUseCase = getVideoStatusUseCase;
        _updateVideoStatusUseCase = updateVideoStatusUseCase;
        _fileStorageService = fileStorageService;
        _logger = logger;
    }

    /// <summary>
    /// Faz upload de um vídeo para processamento
    /// </summary>
    /// <param name="request">Dados do upload</param>
    /// <returns>Resultado do upload</returns>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(VideoUploadResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> UploadVideo([FromForm] VideoUploadRequestDto request)
    {
        if (!ModelState.IsValid)
        {
            _logger.LogWarning("Requisição de upload inválida");
            return BadRequest(ModelState);
        }

        _logger.LogInformation(
            "Recebida requisição de upload. UserId: {UserId}, FileName: {FileName}",
            request.UserId, request.Video.FileName);

        var result = await _uploadVideoUseCase.ExecuteAsync(request);

        if (!result.Success)
        {
            return BadRequest(result);
        }

        return Ok(result);
    }

    /// <summary>
    /// Lista todos os vídeos de um usuário
    /// </summary>
    /// <param name="userId">ID do usuário</param>
    /// <returns>Lista de vídeos do usuário</returns>
    [HttpGet("user/{userId}")]
    [ProducesResponseType(typeof(IEnumerable<VideoInfoDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetUserVideos([FromRoute] string userId)
    {
        if (string.IsNullOrWhiteSpace(userId))
        {
            return BadRequest(new { message = "UserId é obrigatório" });
        }

        _logger.LogInformation("Buscando vídeos do usuário {UserId}", userId);

        var videos = await _getUserVideosUseCase.ExecuteAsync(userId);
        return Ok(videos);
    }

    /// <summary>
    /// Consulta o status de processamento de um vídeo específico
    /// </summary>
    /// <param name="videoId">ID do vídeo</param>
    /// <returns>Informações e status do vídeo</returns>
    [HttpGet("{videoId}/status")]
    [ProducesResponseType(typeof(VideoInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetVideoStatus([FromRoute] string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            return BadRequest(new { message = "VideoId é obrigatório" });
        }

        _logger.LogInformation("Consultando status do vídeo {VideoId}", videoId);

        var video = await _getVideoStatusUseCase.ExecuteAsync(videoId);

        if (video == null)
        {
            return NotFound(new { message = $"Vídeo {videoId} não encontrado" });
        }

        return Ok(video);
    }

    /// <summary>
    /// Download do arquivo ZIP com frames processados
    /// </summary>
    /// <param name="videoId">ID do vídeo</param>
    /// <returns>Arquivo ZIP</returns>
    [HttpGet("{videoId}/download")]
    [ProducesResponseType(typeof(FileContentResult), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadFrames([FromRoute] string videoId)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            return BadRequest(new { message = "VideoId é obrigatório" });
        }

        _logger.LogInformation("Solicitação de download para o vídeo {VideoId}", videoId);

        var video = await _getVideoStatusUseCase.ExecuteAsync(videoId);

        if (video == null)
        {
            return NotFound(new { message = $"Vídeo {videoId} não encontrado" });
        }

        if (video.Status != Domain.Enums.ProcessingStatus.Completed || string.IsNullOrEmpty(video.ZipFileName))
        {
            return BadRequest(new
            {
                message = "Vídeo ainda não foi processado ou processamento falhou",
                status = video.Status.ToString()
            });
        }

        // Constrói o caminho no MinIO: outputs/{videoId}/{zipFileName}
        var zipPath = $"outputs/{videoId}/{video.ZipFileName}";

        // Verifica se o arquivo existe no MinIO
        if (!await _fileStorageService.FileExistsAsync(zipPath))
        {
            _logger.LogWarning("Arquivo ZIP não encontrado no storage: {ZipPath}", zipPath);
            return NotFound(new { message = "Arquivo ZIP não encontrado no storage" });
        }

        try
        {
            // Obtém o stream do arquivo do MinIO
            var fileStream = await _fileStorageService.GetFileStreamAsync(zipPath);
            
            // Retorna o arquivo como download
            return File(fileStream, "application/zip", video.ZipFileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao baixar arquivo do storage: {ZipPath}", zipPath);
            return StatusCode(500, new { message = "Erro ao baixar arquivo do storage" });
        }
    }

    [HttpPut("{videoId}/status")]
    [ProducesResponseType(typeof(VideoInfoDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateVideoStatus(
        [FromRoute] string videoId,
        [FromBody] UpdateVideoStatusRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(videoId))
        {
            return BadRequest(new { message = "VideoId é obrigatório" });
        }

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        _logger.LogInformation(
            "Recebida atualização de status para vídeo {VideoId}: {Status}",
            videoId, request.Status);

        var result = await _updateVideoStatusUseCase.ExecuteAsync(videoId, request);

        if (result == null)
        {
            return NotFound(new { message = $"Vídeo {videoId} não encontrado" });
        }

        return Ok(result);
    }

    [HttpGet("health")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public IActionResult Health()
    {
        return Ok(new
        {
            status = "healthy",
            service = "video-manager-service",
            version = "2.0.0",
            architecture = "clean-architecture",
            timestamp = DateTime.UtcNow
        });
    }
}
