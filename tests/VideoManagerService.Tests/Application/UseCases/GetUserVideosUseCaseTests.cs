using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using VideoManagerService.Application.UseCases;
using VideoManagerService.Domain.Entities;
using VideoManagerService.Domain.Enums;
using VideoManagerService.Domain.Interfaces.Repositories;
using Xunit;

namespace VideoManagerService.Tests.Application.UseCases;

public class GetUserVideosUseCaseTests
{
    private readonly Mock<IVideoRepository> _mockRepository;
    private readonly Mock<ILogger<GetUserVideosUseCase>> _mockLogger;
    private readonly GetUserVideosUseCase _useCase;

    public GetUserVideosUseCaseTests()
    {
        _mockRepository = new Mock<IVideoRepository>();
        _mockLogger = new Mock<ILogger<GetUserVideosUseCase>>();
        _useCase = new GetUserVideosUseCase(_mockRepository.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithExistingVideos_ShouldReturnAll()
    {
        // Arrange
        var userId = "user123";
        var videos = new List<VideoUpload>
        {
            new VideoUpload(userId, "video1.mp4", "stored1.mp4", "/path1", 1000),
            new VideoUpload(userId, "video2.mp4", "stored2.mp4", "/path2", 2000),
            new VideoUpload(userId, "video3.mp4", "stored3.mp4", "/path3", 3000)
        };

        _mockRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(videos);

        // Act
        var result = await _useCase.ExecuteAsync(userId);

        // Assert
        result.Should().HaveCount(3);
        result.Should().AllSatisfy(dto => dto.UserId.Should().Be(userId));
    }

    [Fact]
    public async Task ExecuteAsync_WithNoVideos_ShouldReturnEmptyList()
    {
        // Arrange
        var userId = "user-no-videos";
        _mockRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<VideoUpload>());

        // Act
        var result = await _useCase.ExecuteAsync(userId);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldMapAllPropertiesCorrectly()
    {
        // Arrange
        var userId = "user123";
        var video = new VideoUpload(userId, "video.mp4", "stored_video.mp4", "/path", 5000);
        video.StartProcessing();
        video.CompleteProcessing("frames.zip", 120);

        _mockRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(new List<VideoUpload> { video });

        // Act
        var result = (await _useCase.ExecuteAsync(userId)).ToList();

        // Assert
        var dto = result.First();
        dto.Id.Should().Be(video.Id);
        dto.UserId.Should().Be(video.UserId);
        dto.OriginalFileName.Should().Be(video.OriginalFileName);
        dto.FileSizeBytes.Should().Be(video.FileSizeBytes);
        dto.Status.Should().Be(video.Status);
        dto.FrameCount.Should().Be(video.FrameCount);
        dto.ZipFileName.Should().Be(video.ZipFileName);
        dto.UploadedAt.Should().Be(video.UploadedAt);
    }

    [Fact]
    public async Task ExecuteAsync_WithMixedStatuses_ShouldReturnAll()
    {
        // Arrange
        var userId = "user123";
        var videos = new List<VideoUpload>
        {
            new VideoUpload(userId, "pending.mp4", "stored_pending.mp4", "/path1", 1000),
            new VideoUpload(userId, "processing.mp4", "stored_processing.mp4", "/path2", 2000),
            new VideoUpload(userId, "completed.mp4", "stored_completed.mp4", "/path3", 3000)
        };

        videos[1].StartProcessing();
        videos[2].StartProcessing();
        videos[2].CompleteProcessing("frames.zip", 100);

        _mockRepository.Setup(x => x.GetByUserIdAsync(userId))
            .ReturnsAsync(videos);

        // Act
        var result = await _useCase.ExecuteAsync(userId);

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(dto => dto.Status == ProcessingStatus.Pending);
        result.Should().Contain(dto => dto.Status == ProcessingStatus.Processing);
        result.Should().Contain(dto => dto.Status == ProcessingStatus.Completed);
    }
}
