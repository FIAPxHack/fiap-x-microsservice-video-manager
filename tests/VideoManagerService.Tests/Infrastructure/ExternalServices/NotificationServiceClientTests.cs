using System.Net;
using System.Text.Json;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using VideoManagerService.Infrastructure.ExternalServices;

namespace VideoManagerService.Tests.Infrastructure.ExternalServices;

public class NotificationServiceClientTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly Mock<ILogger<NotificationServiceClient>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly NotificationServiceClient _client;

    public NotificationServiceClientTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _loggerMock = new Mock<ILogger<NotificationServiceClient>>();
        _configurationMock = new Mock<IConfiguration>();

        _configurationMock.Setup(c => c["Services:NotificationService:Url"])
            .Returns("http://localhost:5001");

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _client = new NotificationServiceClient(httpClient, _configurationMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task NotifyProcessingStartedAsync_ShouldSendCorrectRequest_WhenCalled()
    {
        // Arrange
        var userId = "user123";
        var email = "user@example.com";
        var videoName = "TesteVideo.mp4";

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"success\": true}")
            });

        // Act
        await _client.NotifyProcessingStartedAsync(userId, email, videoName);

        // Assert
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.ToString() == "http://localhost:5001/api/notifications/send"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task NotifyProcessingCompletedAsync_ShouldSendCorrectRequest_WhenCalled()
    {
        // Arrange
        var userId = "user123";
        var email = "user@example.com";
        var videoName = "TesteVideo.mp4";
        var frameCount = 120;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"success\": true}")
            });

        // Act
        await _client.NotifyProcessingCompletedAsync(userId, email, videoName, frameCount);

        // Assert
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.ToString() == "http://localhost:5001/api/notifications/send"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task NotifyProcessingFailedAsync_ShouldSendCorrectRequest_WhenCalled()
    {
        // Arrange
        var userId = "user123";
        var email = "user@example.com";
        var videoName = "TesteVideo.mp4";
        var errorMessage = "Formato de vídeo inválido";

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{\"success\": true}")
            });

        // Act
        await _client.NotifyProcessingFailedAsync(userId, email, videoName, errorMessage);

        // Assert
        _httpMessageHandlerMock.Protected().Verify(
            "SendAsync",
            Times.Once(),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Post &&
                req.RequestUri!.ToString() == "http://localhost:5001/api/notifications/send"),
            ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task NotifyProcessingStartedAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var userId = "user123";
        var email = "user@example.com";
        var videoName = "TesteVideo.mp4";

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        await _client.NotifyProcessingStartedAsync(userId, email, videoName);

        // Assert - não deve lançar exceção, apenas logar erro
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyProcessingCompletedAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var userId = "user123";
        var email = "user@example.com";
        var videoName = "TesteVideo.mp4";
        var frameCount = 120;

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        await _client.NotifyProcessingCompletedAsync(userId, email, videoName, frameCount);

        // Assert - não deve lançar exceção, apenas logar erro
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyProcessingFailedAsync_ShouldLogError_WhenExceptionOccurs()
    {
        // Arrange
        var userId = "user123";
        var email = "user@example.com";
        var videoName = "TesteVideo.mp4";
        var errorMessage = "Erro de processamento";

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Network error"));

        // Act
        await _client.NotifyProcessingFailedAsync(userId, email, videoName, errorMessage);

        // Assert - não deve lançar exceção, apenas logar erro
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task NotifyProcessingStartedAsync_ShouldLogWarning_WhenResponseIsNotSuccess()
    {
        // Arrange
        var userId = "user123";
        var email = "user@example.com";
        var videoName = "TesteVideo.mp4";

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest,
                Content = new StringContent("{\"error\": \"Invalid request\"}")
            });

        // Act
        await _client.NotifyProcessingStartedAsync(userId, email, videoName);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public void Constructor_ShouldUseDefaultUrl_WhenConfigurationIsMissing()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["Services:NotificationService:Url"])
            .Returns((string?)null);

        var httpClient = new HttpClient(_httpMessageHandlerMock.Object);

        // Act
        var client = new NotificationServiceClient(httpClient, configMock.Object, _loggerMock.Object);

        // Assert
        client.Should().NotBeNull();
    }
}
