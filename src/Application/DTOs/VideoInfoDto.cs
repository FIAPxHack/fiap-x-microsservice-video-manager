using VideoManagerService.Domain.Enums;

namespace VideoManagerService.Application.DTOs;

/// <summary>
/// DTO para informações do vídeo
/// </summary>
public class VideoInfoDto
{
    public string Id { get; set; } = string.Empty;
    public string UserId { get; set; } = string.Empty;
    public string OriginalFileName { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string FileSizeMB => $"{FileSizeBytes / 1024.0 / 1024.0:F2} MB";
    public ProcessingStatus Status { get; set; }
    public string StatusDescription => GetStatusDescription(Status);
    public DateTime UploadedAt { get; set; }
    public DateTime? ProcessingStartedAt { get; set; }
    public DateTime? ProcessingCompletedAt { get; set; }
    public string? ZipFileName { get; set; }
    public int? FrameCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ProcessingDuration { get; set; }

    private static string GetStatusDescription(ProcessingStatus status)
    {
        return status switch
        {
            ProcessingStatus.Pending => "Aguardando processamento",
            ProcessingStatus.Processing => "Em processamento",
            ProcessingStatus.Completed => "Processamento concluído",
            ProcessingStatus.Failed => "Falha no processamento",
            _ => "Status desconhecido"
        };
    }
}
