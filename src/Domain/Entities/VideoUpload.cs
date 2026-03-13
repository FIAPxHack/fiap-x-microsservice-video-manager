using VideoManagerService.Domain.Enums;

namespace VideoManagerService.Domain.Entities;

/// <summary>
/// Entidade de domínio representando um upload de vídeo
/// </summary>
public class VideoUpload
{
    public string Id { get; private set; }
    public string UserId { get; private set; }
    public string OriginalFileName { get; private set; }
    public string StoredFileName { get; private set; }
    public string FilePath { get; private set; }
    public long FileSizeBytes { get; private set; }
    public ProcessingStatus Status { get; private set; }
    public DateTime UploadedAt { get; private set; }
    public DateTime? ProcessingStartedAt { get; private set; }
    public DateTime? ProcessingCompletedAt { get; private set; }
    public string? ZipFileName { get; private set; }
    public int? FrameCount { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Construtor privado para EF Core
    private VideoUpload()
    {
        Id = string.Empty;
        UserId = string.Empty;
        OriginalFileName = string.Empty;
        StoredFileName = string.Empty;
        FilePath = string.Empty;
    }

    // Construtor público para criação de novos uploads
    public VideoUpload(
        string userId,
        string originalFileName,
        string storedFileName,
        string filePath,
        long fileSizeBytes)
    {
        Id = Guid.NewGuid().ToString();
        UserId = userId ?? throw new ArgumentNullException(nameof(userId));
        OriginalFileName = originalFileName ?? throw new ArgumentNullException(nameof(originalFileName));
        StoredFileName = storedFileName ?? throw new ArgumentNullException(nameof(storedFileName));
        FilePath = filePath ?? throw new ArgumentNullException(nameof(filePath));
        FileSizeBytes = fileSizeBytes;
        Status = ProcessingStatus.Pending;
        UploadedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Inicia o processamento do vídeo
    /// </summary>
    public void StartProcessing()
    {
        if (Status != ProcessingStatus.Pending)
            throw new InvalidOperationException($"Não é possível iniciar processamento. Status atual: {Status}");

        Status = ProcessingStatus.Processing;
        ProcessingStartedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Marca o processamento como concluído
    /// </summary>
    public void CompleteProcessing(string zipFileName, int frameCount)
    {
        if (Status != ProcessingStatus.Processing)
            throw new InvalidOperationException($"Processamento não está em andamento. Status atual: {Status}");

        Status = ProcessingStatus.Completed;
        ProcessingCompletedAt = DateTime.UtcNow;
        ZipFileName = zipFileName;
        FrameCount = frameCount;
    }

    /// <summary>
    /// Marca o processamento como falha
    /// </summary>
    public void MarkAsFailed(string errorMessage)
    {
        Status = ProcessingStatus.Failed;
        ProcessingCompletedAt = DateTime.UtcNow;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Valida se o arquivo é um vídeo válido
    /// </summary>
    public bool HasValidVideoExtension()
    {
        var validExtensions = new[] { ".mp4", ".avi", ".mov", ".mkv", ".wmv", ".flv", ".webm" };
        var extension = Path.GetExtension(OriginalFileName).ToLowerInvariant();
        return validExtensions.Contains(extension);
    }

    /// <summary>
    /// Calcula o tempo de processamento
    /// </summary>
    public TimeSpan? GetProcessingDuration()
    {
        if (ProcessingStartedAt.HasValue && ProcessingCompletedAt.HasValue)
        {
            return ProcessingCompletedAt.Value - ProcessingStartedAt.Value;
        }
        return null;
    }
}
