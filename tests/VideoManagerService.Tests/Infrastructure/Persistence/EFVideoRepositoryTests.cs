using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VideoManagerService.Domain.Entities;
using VideoManagerService.Domain.Enums;
using VideoManagerService.Infrastructure.Data;
using VideoManagerService.Infrastructure.Persistence;

namespace VideoManagerService.Tests.Infrastructure.Persistence;

public class EFVideoRepositoryTests : IDisposable
{
    private readonly VideoManagerDbContext _context;
    private readonly EFVideoRepository _repository;

    public EFVideoRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<VideoManagerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new VideoManagerDbContext(options);
        _repository = new EFVideoRepository(_context);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public async Task SaveAsync_ShouldSaveVideo_WhenVideoIsValid()
    {
        // Arrange
        var video = CreateTestVideo();

        // Act
        var result = await _repository.SaveAsync(video);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(video.Id);
        
        var savedVideo = await _context.VideoUploads.FindAsync(video.Id);
        savedVideo.Should().NotBeNull();
        savedVideo!.OriginalFileName.Should().Be(video.OriginalFileName);
    }

    [Fact]
    public async Task SaveAsync_ShouldThrowArgumentNullException_WhenVideoIsNull()
    {
        // Act
        Func<Task> act = async () => await _repository.SaveAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnVideo_WhenVideoExists()
    {
        // Arrange
        var video = CreateTestVideo();
        await _context.VideoUploads.AddAsync(video);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByIdAsync(video.Id);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(video.Id);
        result.OriginalFileName.Should().Be(video.OriginalFileName);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenVideoDoesNotExist()
    {
        // Act
        var result = await _repository.GetByIdAsync("non-existent-id");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdIsNull()
    {
        // Act
        var result = await _repository.GetByIdAsync(null!);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_ShouldReturnNull_WhenIdIsWhiteSpace()
    {
        // Act
        var result = await _repository.GetByIdAsync("   ");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnUserVideos_WhenVideosExist()
    {
        // Arrange
        var userId = "user123";
        var video1 = CreateTestVideo("vid1", userId);
        var video2 = CreateTestVideo("vid2", userId);
        var video3 = CreateTestVideo("vid3", "otherUser");

        await _context.VideoUploads.AddRangeAsync(video1, video2, video3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByUserIdAsync(userId);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(v => v.UserId == userId);
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnEmptyList_WhenUserHasNoVideos()
    {
        // Act
        var result = await _repository.GetByUserIdAsync("non-existent-user");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnEmptyList_WhenUserIdIsNull()
    {
        // Act
        var result = await _repository.GetByUserIdAsync(null!);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnEmptyList_WhenUserIdIsWhiteSpace()
    {
        // Act
        var result = await _repository.GetByUserIdAsync("   ");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByUserIdAsync_ShouldReturnVideosOrderedByUploadedAtDescending()
    {
        // Arrange
        var userId = "user123";
        var video1 = CreateTestVideo("vid1", userId);
        video1.GetType().GetProperty("UploadedAt")!.SetValue(video1, DateTime.UtcNow.AddHours(-2));
        
        var video2 = CreateTestVideo("vid2", userId);
        video2.GetType().GetProperty("UploadedAt")!.SetValue(video2, DateTime.UtcNow.AddHours(-1));

        await _context.VideoUploads.AddRangeAsync(video1, video2);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetByUserIdAsync(userId)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(video2.Id); // mais recente primeiro
        result[1].Id.Should().Be(video1.Id);
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateVideo_WhenVideoExists()
    {
        // Arrange
        var video = CreateTestVideo();
        await _context.VideoUploads.AddAsync(video);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Modificar o vídeo
        video.StartProcessing();
        video.CompleteProcessing("frames.zip", 120);

        // Act
        await _repository.UpdateAsync(video);

        // Assert
        var updatedVideo = await _context.VideoUploads.FindAsync(video.Id);
        updatedVideo.Should().NotBeNull();
        updatedVideo!.Status.Should().Be(ProcessingStatus.Completed);
        updatedVideo.FrameCount.Should().Be(120);
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowArgumentNullException_WhenVideoIsNull()
    {
        // Act
        Func<Task> act = async () => await _repository.UpdateAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldDeleteVideo_WhenVideoExists()
    {
        // Arrange
        var video = CreateTestVideo();
        await _context.VideoUploads.AddAsync(video);
        await _context.SaveChangesAsync();

        // Act
        await _repository.DeleteAsync(video.Id);

        // Assert
        var deletedVideo = await _context.VideoUploads.FindAsync(video.Id);
        deletedVideo.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenVideoDoesNotExist()
    {
        // Act
        Func<Task> act = async () => await _repository.DeleteAsync("non-existent-id");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenIdIsNull()
    {
        // Act
        Func<Task> act = async () => await _repository.DeleteAsync(null!);

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteAsync_ShouldNotThrow_WhenIdIsWhiteSpace()
    {
        // Act
        Func<Task> act = async () => await _repository.DeleteAsync("   ");

        // Assert
        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnVideosWithStatus_WhenVideosExist()
    {
        // Arrange
        var video1 = CreateTestVideo("vid1");
        var video2 = CreateTestVideo("vid2");
        video2.StartProcessing();
        video2.CompleteProcessing("frames.zip", 100);
        var video3 = CreateTestVideo("vid3");

        await _context.VideoUploads.AddRangeAsync(video1, video2, video3);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(ProcessingStatus.Pending);

        // Assert
        result.Should().HaveCount(2);
        result.Should().OnlyContain(v => v.Status == ProcessingStatus.Pending);
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnEmptyList_WhenNoVideosWithStatus()
    {
        // Arrange
        var video = CreateTestVideo();
        await _context.VideoUploads.AddAsync(video);
        await _context.SaveChangesAsync();

        // Act
        var result = await _repository.GetByStatusAsync(ProcessingStatus.Completed);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetByStatusAsync_ShouldReturnVideosOrderedByUploadedAt()
    {
        // Arrange
        var video1 = CreateTestVideo("vid1");
        video1.GetType().GetProperty("UploadedAt")!.SetValue(video1, DateTime.UtcNow.AddHours(-2));
        
        var video2 = CreateTestVideo("vid2");
        video2.GetType().GetProperty("UploadedAt")!.SetValue(video2, DateTime.UtcNow.AddHours(-1));

        await _context.VideoUploads.AddRangeAsync(video1, video2);
        await _context.SaveChangesAsync();

        // Act
        var result = (await _repository.GetByStatusAsync(ProcessingStatus.Pending)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].Id.Should().Be(video1.Id); // mais antigo primeiro
        result[1].Id.Should().Be(video2.Id);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenContextIsNull()
    {
        // Act
        Action act = () => new EFVideoRepository(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    private static VideoUpload CreateTestVideo(string? id = null, string? userId = null)
    {
        return new VideoUpload(
            id ?? Guid.NewGuid().ToString(),
            userId ?? "testUser",
            "user@example.com",
            "test-video.mp4",
            "stored-video.mp4",
            "/videos/stored-video.mp4",
            1024000
        );
    }
}
