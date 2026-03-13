namespace VideoManagerService.Domain.Enums;

/// <summary>
/// Status do processamento de vídeo
/// </summary>
public enum ProcessingStatus
{
    /// <summary>
    /// Vídeo aguardando processamento
    /// </summary>
    Pending,
    
    /// <summary>
    /// Processamento em andamento
    /// </summary>
    Processing,
    
    /// <summary>
    /// Processamento concluído com sucesso
    /// </summary>
    Completed,
    
    /// <summary>
    /// Falha no processamento
    /// </summary>
    Failed
}
