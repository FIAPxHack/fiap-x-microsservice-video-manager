namespace VideoManagerService.Application.DTOs;

/// <summary>
/// DTO para resposta de upload de vídeo
/// </summary>
public class VideoUploadResponseDto
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public string? VideoId { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
