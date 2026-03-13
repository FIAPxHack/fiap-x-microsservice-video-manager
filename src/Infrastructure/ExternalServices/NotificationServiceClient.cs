using System.Text;
using System.Text.Json;
using VideoManagerService.Domain.Interfaces.Services;

namespace VideoManagerService.Infrastructure.ExternalServices;

/// <summary>
/// Cliente HTTP para integração com o microserviço de notificações
/// </summary>
public class NotificationServiceClient : INotificationServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<NotificationServiceClient> _logger;
    private readonly string _notificationServiceUrl;

    public NotificationServiceClient(
        HttpClient httpClient,
        IConfiguration configuration,
        ILogger<NotificationServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
        _notificationServiceUrl = configuration["Services:NotificationService:Url"]
            ?? "http://localhost:5001";
    }

    public async Task NotifyProcessingStartedAsync(string userId, string email, string videoName)
    {
        try
        {
            var request = new
            {
                userId,
                email,
                subject = $"🎬 Processamento Iniciado - {videoName}",
                message = $"O processamento do vídeo '{videoName}' foi iniciado. Você será notificado quando estiver concluído.",
                type = 0 // VideoProcessingStarted
            };

            await SendNotificationAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao notificar início de processamento para {UserId}", userId);
        }
    }

    public async Task NotifyProcessingCompletedAsync(string userId, string email, string videoName, int frameCount)
    {
        try
        {
            var request = new
            {
                userId,
                email,
                subject = $"✅ Processamento Concluído - {videoName}",
                message = $"O processamento do vídeo '{videoName}' foi concluído com sucesso! {frameCount} frames foram extraídos e estão disponíveis para download.",
                type = 1 // VideoProcessingCompleted
            };

            await SendNotificationAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao notificar conclusão de processamento para {UserId}", userId);
        }
    }

    public async Task NotifyProcessingFailedAsync(string userId, string email, string videoName, string errorMessage)
    {
        try
        {
            var request = new
            {
                userId,
                email,
                subject = $"❌ Falha no Processamento - {videoName}",
                message = $"Ocorreu uma falha no processamento do vídeo '{videoName}'. Erro: {errorMessage}",
                type = 2 // VideoProcessingFailed
            };

            await SendNotificationAsync(request);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao notificar falha de processamento para {UserId}", userId);
        }
    }

    private async Task SendNotificationAsync(object request)
    {
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var url = $"{_notificationServiceUrl}/api/notifications/send";

        _logger.LogInformation("Enviando notificação para {Url}", url);

        var response = await _httpClient.PostAsync(url, content);

        if (response.IsSuccessStatusCode)
        {
            _logger.LogInformation("Notificação enviada com sucesso");
        }
        else
        {
            var errorContent = await response.Content.ReadAsStringAsync();
            _logger.LogWarning(
                "Falha ao enviar notificação. Status: {StatusCode}, Resposta: {Response}",
                response.StatusCode, errorContent);
        }
    }
}
