using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VideoManagerService.Application.UseCases;
using VideoManagerService.Domain.Entities;
using VideoManagerService.Domain.Enums;
using VideoManagerService.Domain.Interfaces.Repositories;
using Xunit;

namespace VideoManagerService.Tests.Application.UseCases;

public class GetVideoStatusUseCaseTests
{
    private readonly Mock<IVideoRepository> _mockRepository;
    private readonly Mock<ILogger<GetVideoStatusUseCase>> _mockLogger;
    private readonly GetVideoStatusUseCase _useCase;

    public GetVideoStatusUseCaseTests()
    {
        _mockRepository = new Mock<IVideoRepository>();
        _mockLogger = new Mock<ILogger<GetVideoStatusUseCase>>();
        _useCase = new GetVideoStatusUseCase(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingVideo_ShouldReturnVideoInfo()
    {
        // Arrange
        var videoId = "video123";
        var video = new VideoUpload("user1", "video.mp4", "stored_video.mp4", "/path", 5000);
        video.StartProcessing();
        video.CompleteProcessing("frames.zip", 120);

        _mockRepository.Setup(x => x.GetByIdAsync(videoId))
            .ReturnsAsync(video);

        // Act
        var result = await _useCase.ExecuteAsync(videoId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(video.Id);
        result.Status.Should().Be(ProcessingStatus.Completed);
        result.FrameCount.Should().Be(120);
        result.ZipFileName.Should().Be("frames.zip");
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistingVideo_ShouldReturnNull()
    {
        // Arrange
        var videoId = "non-existing";
        _mockRepository.Setup(x => x.GetByIdAsync(videoId))
            .ReturnsAsync((VideoUpload?)null);

        // Act
        var result = await _useCase.ExecuteAsync(videoId);

        // Assert
        result.Should().BeNull();
    }

    [Theory]
    [InlineData(ProcessingStatus.Pending)]
    [InlineData(ProcessingStatus.Processing)]
    [InlineData(ProcessingStatus.Completed)]
    [InlineData(ProcessingStatus.Failed)]
    public async Task ExecuteAsync_WithDifferentStatuses_ShouldReturnCorrectStatus(ProcessingStatus status)
    {
        // Arrange
        var videoId = "video123";
        var video = new VideoUpload("user1", "video.mp4", "stored_video.mp4", "/path", 5000);

        if (status == ProcessingStatus.Processing)
            video.StartProcessing();
        else if (status == ProcessingStatus.Completed)
        {
            video.StartProcessing();
            video.CompleteProcessing("frames.zip", 100);
        }
        else if (status == ProcessingStatus.Failed)
        {
            video.StartProcessing();
            video.MarkAsFailed("Error");
        }

        _mockRepository.Setup(x => x.GetByIdAsync(videoId))
            .ReturnsAsync(video);

        // Act
        var result = await _useCase.ExecuteAsync(videoId);

        // Assert
        result!.Status.Should().Be(status);
    }
}
