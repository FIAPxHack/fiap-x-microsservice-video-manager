namespace VideoManagerService.Domain.Interfaces.Services;

/// <summary>
/// Interface para envio de notificações (integração com microserviço de notificações)
/// </summary>
public interface INotificationServiceClient
{
    /// <summary>
    /// Notifica o usuário sobre início de processamento
    /// </summary>
    Task NotifyProcessingStartedAsync(string userId, string email, string videoName);
    
    /// <summary>
    /// Notifica o usuário sobre conclusão de processamento
    /// </summary>
    Task NotifyProcessingCompletedAsync(string userId, string email, string videoName, int frameCount);
    
    /// <summary>
    /// Notifica o usuário sobre falha no processamento
    /// </summary>
    Task NotifyProcessingFailedAsync(string userId, string email, string videoName, string errorMessage);
}
