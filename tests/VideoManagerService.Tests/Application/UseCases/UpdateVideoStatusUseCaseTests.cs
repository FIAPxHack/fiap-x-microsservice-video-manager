using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VideoManagerService.Application.DTOs;
using VideoManagerService.Application.UseCases;
using VideoManagerService.Domain.Entities;
using VideoManagerService.Domain.Enums;
using VideoManagerService.Domain.Interfaces.Repositories;
using VideoManagerService.Domain.Interfaces.Services;
using Xunit;

namespace VideoManagerService.Tests.Application.UseCases;

public class UpdateVideoStatusUseCaseTests
{
    private readonly Mock<IVideoRepository> _mockRepository;
    private readonly Mock<INotificationServiceClient> _mockNotificationClient;
    private readonly Mock<ILogger<UpdateVideoStatusUseCase>> _mockLogger;
    private readonly UpdateVideoStatusUseCase _useCase;

    public UpdateVideoStatusUseCaseTests()
    {
        _mockRepository = new Mock<IVideoRepository>();
        _mockNotificationClient = new Mock<INotificationServiceClient>();
        _mockLogger = new Mock<ILogger<UpdateVideoStatusUseCase>>();
        _useCase = new UpdateVideoStatusUseCase(
            _mockRepository.Object,
            _mockNotificationClient.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentVideo_ShouldReturnNull()
    {
        // Arrange
        var videoId = "non-existent";
        var request = new UpdateVideoStatusRequestDto { Status = ProcessingStatus.Processing };

        _mockRepository.Setup(x => x.GetByIdAsync(videoId))
            .ReturnsAsync((VideoUpload?)null);

        // Act
        var result = await _useCase.ExecuteAsync(videoId, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithProcessingStatus_ShouldUpdateToProcessing()
    {
        // Arrange
        var videoId = "video-123";
        var video = new VideoUpload("user-123", "test.mp4", "test.mp4", "/path/test.mp4", 1024);
        var request = new UpdateVideoStatusRequestDto { Status = ProcessingStatus.Processing };

        _mockRepository.Setup(x => x.GetByIdAsync(videoId)).ReturnsAsync(video);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<VideoUpload>())).Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(videoId, request);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(ProcessingStatus.Processing);
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<VideoUpload>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompletedStatus_ShouldCompleteProcessing()
    {
        // Arrange
        var videoId = "video-123";
        var video = new VideoUpload("user-123", "test.mp4", "test.mp4", "/path/test.mp4", 1024);
        video.StartProcessing();
        
        var request = new UpdateVideoStatusRequestDto 
        { 
            Status = ProcessingStatus.Completed,
            ZipFileName = "output.zip",
            FrameCount = 100
        };

        _mockRepository.Setup(x => x.GetByIdAsync(videoId)).ReturnsAsync(video);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<VideoUpload>())).Returns(Task.CompletedTask);
        _mockNotificationClient.Setup(x => x.NotifyProcessingCompletedAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(videoId, request);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(ProcessingStatus.Completed);
        result.ZipFileName.Should().Be("output.zip");
        result.FrameCount.Should().Be(100);
        _mockNotificationClient.Verify(x => x.NotifyProcessingCompletedAsync(
            "user-123", "user@example.com", "test.mp4", 100), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithCompletedStatusAndNoZipFileName_ShouldReturnNull()
    {
        // Arrange
        var videoId = "video-123";
        var video = new VideoUpload("user-123", "test.mp4", "test.mp4", "/path/test.mp4", 1024);
        var request = new UpdateVideoStatusRequestDto 
        { 
            Status = ProcessingStatus.Completed,
            ZipFileName = null
        };

        _mockRepository.Setup(x => x.GetByIdAsync(videoId)).ReturnsAsync(video);

        // Act
        var result = await _useCase.ExecuteAsync(videoId, request);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedStatus_ShouldMarkAsFailed()
    {
        // Arrange
        var videoId = "video-123";
        var video = new VideoUpload("user-123", "test.mp4", "test.mp4", "/path/test.mp4", 1024);
        video.StartProcessing();
        
        var request = new UpdateVideoStatusRequestDto 
        { 
            Status = ProcessingStatus.Failed,
            ErrorMessage = "Processing error occurred"
        };

        _mockRepository.Setup(x => x.GetByIdAsync(videoId)).ReturnsAsync(video);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<VideoUpload>())).Returns(Task.CompletedTask);
        _mockNotificationClient.Setup(x => x.NotifyProcessingFailedAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _useCase.ExecuteAsync(videoId, request);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(ProcessingStatus.Failed);
        result.ErrorMessage.Should().Be("Processing error occurred");
        _mockNotificationClient.Verify(x => x.NotifyProcessingFailedAsync(
            "user-123", "user@example.com", "test.mp4", "Processing error occurred"), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_WithNotificationFailure_ShouldStillUpdateVideo()
    {
        // Arrange
        var videoId = "video-123";
        var video = new VideoUpload("user-123", "test.mp4", "test.mp4", "/path/test.mp4", 1024);
        video.StartProcessing();
        
        var request = new UpdateVideoStatusRequestDto 
        { 
            Status = ProcessingStatus.Completed,
            ZipFileName = "output.zip",
            FrameCount = 100
        };

        _mockRepository.Setup(x => x.GetByIdAsync(videoId)).ReturnsAsync(video);
        _mockRepository.Setup(x => x.UpdateAsync(It.IsAny<VideoUpload>())).Returns(Task.CompletedTask);
        _mockNotificationClient.Setup(x => x.NotifyProcessingCompletedAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>()))
            .ThrowsAsync(new Exception("Notification service unavailable"));

        // Act
        var result = await _useCase.ExecuteAsync(videoId, request);

        // Assert
        result.Should().NotBeNull();
        result!.Status.Should().Be(ProcessingStatus.Completed);
        _mockRepository.Verify(x => x.UpdateAsync(It.IsAny<VideoUpload>()), Times.Once);
    }
}
