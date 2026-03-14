using FluentAssertions;
using VideoManagerService.Domain.Entities;
using VideoManagerService.Domain.Enums;
using VideoManagerService.Infrastructure.Persistence;
using Xunit;

namespace VideoManagerService.Tests.Infrastructure.Persistence;

public class InMemoryVideoRepositoryTests
{
    private readonly InMemoryVideoRepository _repository;

    public InMemoryVideoRepositoryTests()
    {
        _repository = new InMemoryVideoRepository();
    }

    [Fact]
    public async Task SaveAsync_WithValidVideo_ShouldSaveAndReturn()
    {
        // Arrange
        var video = new VideoUpload("user-123", "test.mp4", "test.mp4", "/path/test.mp4", 1024);

        // Act
        var result = await _repository.SaveAsync(video);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(video.Id);
    }

    [Fact]
    public async Task SaveAsync_WithNullVideo_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _repository.SaveAsync(null!));
    }

    [Fact]
    public async Task GetByIdAsync_WithExistingVideo_ShouldReturnVideo()
    {
        // Arrange
        var video = new VideoUpload("user-123", "test.mp4", "test.mp4", "/path/test.mp4", 1024);
        await _repository.SaveAsync(video);

        // Act
        var result = await _repository.GetByIdAsync(video.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(video.Id);
        result.OriginalFileName.Should().Be("test.mp4");
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentVideo_ShouldReturnNull()
    {
        // Act
        var result = await _repository.GetByIdAsync("non-existent");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserIdAsync_WithMultipleVideos_ShouldReturnUserVideosInDescendingOrder()
    {
        // Arrange
        var video1 = new VideoUpload("user-123", "video1.mp4", "video1.mp4", "/path/video1.mp4", 1024);
        await _repository.SaveAsync(video1);
        
        await Task.Delay(10); // Ensure different timestamps
        
        var video2 = new VideoUpload("user-123", "video2.mp4", "video2.mp4", "/path/video2.mp4", 2048);
        await _repository.SaveAsync(video2);
        
        var video3 = new VideoUpload("user-456", "video3.mp4", "video3.mp4", "/path/video3.mp4", 3072);
        await _repository.SaveAsync(video3);

        // Act
        var result = await _repository.GetByUserIdAsync("user-123");

        // Assert
        var videos = result.ToList();
        videos.Should().HaveCount(2);
        videos[0].OriginalFileName.Should().Be("video2.mp4"); // More recent
        videos[1].OriginalFileName.Should().Be("video1.mp4");
    }

    [Fact]
    public async Task GetByUserIdAsync_WithNoVideos_ShouldReturnEmptyList()
    {
        // Act
        var result = await _repository.GetByUserIdAsync("non-existent-user");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task UpdateAsync_WithExistingVideo_ShouldUpdateVideo()
    {
        // Arrange
        var video = new VideoUpload("user-123", "test.mp4", "test.mp4", "/path/test.mp4", 1024);
        await _repository.SaveAsync(video);
        
        video.StartProcessing();

        // Act
        await _repository.UpdateAsync(video);
        var updatedVideo = await _repository.GetByIdAsync(video.Id);

        // Assert
        updatedVideo.Should().NotBeNull();
        updatedVideo!.Status.Should().Be(ProcessingStatus.Processing);
    }

    [Fact]
    public async Task UpdateAsync_WithNullVideo_ShouldThrowArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(() => 
            _repository.UpdateAsync(null!));
    }

    [Fact]
    public async Task DeleteAsync_WithExistingVideo_ShouldRemoveVideo()
    {
        // Arrange
        var video = new VideoUpload("user-123", "test.mp4", "test.mp4", "/path/test.mp4", 1024);
        await _repository.SaveAsync(video);

        // Act
        await _repository.DeleteAsync(video.Id);
        var result = await _repository.GetByIdAsync(video.Id);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentVideo_ShouldNotThrow()
    {
        // Act
        var act = async () => await _repository.DeleteAsync("non-existent");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetByStatusAsync_WithMultipleStatuses_ShouldReturnOnlyMatchingVideos()
    {
        // Arrange
        var video1 = new VideoUpload("user-123", "video1.mp4", "video1.mp4", "/path/video1.mp4", 1024);
        await _repository.SaveAsync(video1);
        
        var video2 = new VideoUpload("user-123", "video2.mp4", "video2.mp4", "/path/video2.mp4", 2048);
        video2.StartProcessing();
        await _repository.SaveAsync(video2);
        
        var video3 = new VideoUpload("user-123", "video3.mp4", "video3.mp4", "/path/video3.mp4", 3072);
        video3.StartProcessing();
        video3.CompleteProcessing("output.zip", 100);
        await _repository.SaveAsync(video3);

        // Act
        var processingVideos = await _repository.GetByStatusAsync(ProcessingStatus.Processing);
        var pendingVideos = await _repository.GetByStatusAsync(ProcessingStatus.Pending);
        var completedVideos = await _repository.GetByStatusAsync(ProcessingStatus.Completed);

        // Assert
        processingVideos.Should().HaveCount(1);
        processingVideos.First().OriginalFileName.Should().Be("video2.mp4");
        
        pendingVideos.Should().HaveCount(1);
        pendingVideos.First().OriginalFileName.Should().Be("video1.mp4");
        
        completedVideos.Should().HaveCount(1);
        completedVideos.First().OriginalFileName.Should().Be("video3.mp4");
    }

    [Fact]
    public async Task GetByStatusAsync_WithNoMatchingVideos_ShouldReturnEmptyList()
    {
        // Arrange
        var video = new VideoUpload("user-123", "test.mp4", "test.mp4", "/path/test.mp4", 1024);
        await _repository.SaveAsync(video);

        // Act
        var result = await _repository.GetByStatusAsync(ProcessingStatus.Failed);

        // Assert
        result.Should().BeEmpty();
    }
}
