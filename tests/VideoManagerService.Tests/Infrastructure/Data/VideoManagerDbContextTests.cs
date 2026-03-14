using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VideoManagerService.Domain.Entities;
using VideoManagerService.Domain.Enums;
using VideoManagerService.Infrastructure.Data;

namespace VideoManagerService.Tests.Infrastructure.Data;

public class VideoManagerDbContextTests : IDisposable
{
    private readonly VideoManagerDbContext _context;

    public VideoManagerDbContextTests()
    {
        var options = new DbContextOptionsBuilder<VideoManagerDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new VideoManagerDbContext(options);
    }

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }

    [Fact]
    public void VideoUploads_ShouldNotBeNull()
    {
        // Assert
        _context.VideoUploads.Should().NotBeNull();
    }

    [Fact]
    public async Task VideoUploads_ShouldAddAndRetrieveVideo_WhenVideoIsValid()
    {
        // Arrange
        var video = new VideoUpload(
            "test-id",
            "user123",
            "user@example.com",
            "test-video.mp4",
            "stored-video.mp4",
            "/videos/stored-video.mp4",
            1024000
        );

        // Act
        await _context.VideoUploads.AddAsync(video);
        await _context.SaveChangesAsync();

        var retrievedVideo = await _context.VideoUploads.FindAsync("test-id");

        // Assert
        retrievedVideo.Should().NotBeNull();
        retrievedVideo!.Id.Should().Be("test-id");
        retrievedVideo.UserId.Should().Be("user123");
        retrievedVideo.OriginalFileName.Should().Be("test-video.mp4");
    }

    [Fact]
    public async Task VideoUploads_ShouldApplyConfiguration_WhenSavingVideo()
    {
        // Arrange
        var video = new VideoUpload(
            "test-id",
            "user123",
            "user@example.com",
            "test-video.mp4",
            "stored-video.mp4",
            "/videos/stored-video.mp4",
            1024000
        );

        // Act
        await _context.VideoUploads.AddAsync(video);
        await _context.SaveChangesAsync();

        // Assert - verifica que a configuração foi aplicada
        var entity = _context.Model.FindEntityType(typeof(VideoUpload));
        entity.Should().NotBeNull();
        entity!.GetTableName().Should().Be("video_uploads");
    }

    [Fact]
    public void ModelCreation_ShouldApplyConfigurationsFromAssembly()
    {
        // Act
        var model = _context.Model;

        // Assert
        var videoUploadEntity = model.FindEntityType(typeof(VideoUpload));
        videoUploadEntity.Should().NotBeNull();
        
        // Verifica que a configuração foi aplicada
        var idProperty = videoUploadEntity!.FindProperty("Id");
        idProperty.Should().NotBeNull();
        idProperty!.IsKey().Should().BeTrue();
    }

    [Fact]
    public async Task VideoUploads_ShouldStoreEnumAsString_WhenSavingVideo()
    {
        // Arrange
        var video = new VideoUpload(
            "test-id",
            "user123",
            "user@example.com",
            "test-video.mp4",
            "stored-video.mp4",
            "/videos/stored-video.mp4",
            1024000
        );

        // Act
        await _context.VideoUploads.AddAsync(video);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var retrievedVideo = await _context.VideoUploads.FindAsync("test-id");

        // Assert
        retrievedVideo.Should().NotBeNull();
        retrievedVideo!.Status.Should().Be(ProcessingStatus.Pending);
    }

    [Fact]
    public async Task VideoUploads_ShouldUpdateVideo_WhenVideoIsModified()
    {
        // Arrange
        var video = new VideoUpload(
            "test-id",
            "user123",
            "user@example.com",
            "test-video.mp4",
            "stored-video.mp4",
            "/videos/stored-video.mp4",
            1024000
        );

        await _context.VideoUploads.AddAsync(video);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        // Act
        var retrievedVideo = await _context.VideoUploads.FindAsync("test-id");
        retrievedVideo!.StartProcessing();
        retrievedVideo.CompleteProcessing("frames.zip", 100);
        await _context.SaveChangesAsync();
        _context.ChangeTracker.Clear();

        var updatedVideo = await _context.VideoUploads.FindAsync("test-id");

        // Assert
        updatedVideo.Should().NotBeNull();
        updatedVideo!.Status.Should().Be(ProcessingStatus.Completed);
        updatedVideo.FrameCount.Should().Be(100);
    }

    [Fact]
    public async Task VideoUploads_ShouldDeleteVideo_WhenVideoIsRemoved()
    {
        // Arrange
        var video = new VideoUpload(
            "test-id",
            "user123",
            "user@example.com",
            "test-video.mp4",
            "stored-video.mp4",
            "/videos/stored-video.mp4",
            1024000
        );

        await _context.VideoUploads.AddAsync(video);
        await _context.SaveChangesAsync();

        // Act
        _context.VideoUploads.Remove(video);
        await _context.SaveChangesAsync();

        var deletedVideo = await _context.VideoUploads.FindAsync("test-id");

        // Assert
        deletedVideo.Should().BeNull();
    }

    [Fact]
    public async Task VideoUploads_ShouldQueryByStatus_WhenFiltering()
    {
        // Arrange
        var video1 = new VideoUpload("id1", "user123", "user@example.com", "video1.mp4", "stored1.mp4", "/path1", 1000);
        var video2 = new VideoUpload("id2", "user123", "user@example.com", "video2.mp4", "stored2.mp4", "/path2", 2000);
        video2.StartProcessing();
        video2.CompleteProcessing("frames.zip", 100);

        await _context.VideoUploads.AddRangeAsync(video1, video2);
        await _context.SaveChangesAsync();

        // Act
        var pendingVideos = await _context.VideoUploads
            .Where(v => v.Status == ProcessingStatus.Pending)
            .ToListAsync();

        // Assert
        pendingVideos.Should().HaveCount(1);
        pendingVideos.First().Id.Should().Be("id1");
    }

    [Fact]
    public async Task VideoUploads_ShouldQueryByUserId_WhenFiltering()
    {
        // Arrange
        var video1 = new VideoUpload("id1", "user123", "user1@example.com", "video1.mp4", "stored1.mp4", "/path1", 1000);
        var video2 = new VideoUpload("id2", "user456", "user2@example.com", "video2.mp4", "stored2.mp4", "/path2", 2000);

        await _context.VideoUploads.AddRangeAsync(video1, video2);
        await _context.SaveChangesAsync();

        // Act
        var userVideos = await _context.VideoUploads
            .Where(v => v.UserId == "user123")
            .ToListAsync();

        // Assert
        userVideos.Should().HaveCount(1);
        userVideos.First().Id.Should().Be("id1");
    }
}
