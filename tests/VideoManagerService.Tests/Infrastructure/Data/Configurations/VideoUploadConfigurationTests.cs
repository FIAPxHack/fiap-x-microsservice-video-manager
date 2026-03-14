using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using VideoManagerService.Domain.Entities;
using VideoManagerService.Infrastructure.Data;
using VideoManagerService.Infrastructure.Data.Configurations;

namespace VideoManagerService.Tests.Infrastructure.Data.Configurations;

public class VideoUploadConfigurationTests : IDisposable
{
    private readonly VideoManagerDbContext _context;

    public VideoUploadConfigurationTests()
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
    public void Configure_ShouldSetTableName_ToVideoUploads()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));

        // Assert
        entityType.Should().NotBeNull();
        entityType!.GetTableName().Should().Be("video_uploads");
    }

    [Fact]
    public void Configure_ShouldConfigurePrimaryKey()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var idProperty = entityType!.FindProperty("Id");

        // Assert
        idProperty.Should().NotBeNull();
        idProperty!.IsKey().Should().BeTrue();
        idProperty.GetMaxLength().Should().Be(50);
        idProperty.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Configure_ShouldSetMaxLengthForUserId()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var userIdProperty = entityType!.FindProperty("UserId");

        // Assert
        userIdProperty.Should().NotBeNull();
        userIdProperty!.GetMaxLength().Should().Be(100);
        userIdProperty.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Configure_ShouldSetMaxLengthForOriginalFileName()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var property = entityType!.FindProperty("OriginalFileName");

        // Assert
        property.Should().NotBeNull();
        property!.GetMaxLength().Should().Be(500);
        property.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Configure_ShouldSetMaxLengthForStoredFileName()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var property = entityType!.FindProperty("StoredFileName");

        // Assert
        property.Should().NotBeNull();
        property!.GetMaxLength().Should().Be(500);
        property.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Configure_ShouldSetMaxLengthForFilePath()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var property = entityType!.FindProperty("FilePath");

        // Assert
        property.Should().NotBeNull();
        property!.GetMaxLength().Should().Be(1000);
        property.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Configure_ShouldSetFileSizeBytesAsRequired()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var property = entityType!.FindProperty("FileSizeBytes");

        // Assert
        property.Should().NotBeNull();
        property!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Configure_ShouldConvertStatusToString()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var property = entityType!.FindProperty("Status");

        // Assert
        property.Should().NotBeNull();
        property!.GetMaxLength().Should().Be(20);
        property.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Configure_ShouldSetUploadedAtAsRequired()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var property = entityType!.FindProperty("UploadedAt");

        // Assert
        property.Should().NotBeNull();
        property!.IsNullable.Should().BeFalse();
    }

    [Fact]
    public void Configure_ShouldSetProcessingStartedAtAsOptional()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var property = entityType!.FindProperty("ProcessingStartedAt");

        // Assert
        property.Should().NotBeNull();
        property!.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Configure_ShouldSetProcessingCompletedAtAsOptional()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var property = entityType!.FindProperty("ProcessingCompletedAt");

        // Assert
        property.Should().NotBeNull();
        property!.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Configure_ShouldSetZipFileNameAsOptional()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var property = entityType!.FindProperty("ZipFileName");

        // Assert
        property.Should().NotBeNull();
        property!.GetMaxLength().Should().Be(500);
        property.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Configure_ShouldSetFrameCountAsOptional()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var property = entityType!.FindProperty("FrameCount");

        // Assert
        property.Should().NotBeNull();
        property!.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Configure_ShouldSetErrorMessageAsOptional()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var property = entityType!.FindProperty("ErrorMessage");

        // Assert
        property.Should().NotBeNull();
        property!.GetMaxLength().Should().Be(2000);
        property.IsNullable.Should().BeTrue();
    }

    [Fact]
    public void Configure_ShouldCreateIndexOnUserId()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var indexes = entityType!.GetIndexes();

        // Assert
        var userIdIndex = indexes.FirstOrDefault(i => 
            i.GetDatabaseName() == "IX_VideoUploads_UserId");
        
        userIdIndex.Should().NotBeNull();
    }

    [Fact]
    public void Configure_ShouldCreateIndexOnStatus()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var indexes = entityType!.GetIndexes();

        // Assert
        var statusIndex = indexes.FirstOrDefault(i => 
            i.GetDatabaseName() == "IX_VideoUploads_Status");
        
        statusIndex.Should().NotBeNull();
    }

    [Fact]
    public void Configure_ShouldCreateCompositeIndexOnUserIdAndUploadedAt()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var indexes = entityType!.GetIndexes();

        // Assert
        var compositeIndex = indexes.FirstOrDefault(i => 
            i.GetDatabaseName() == "IX_VideoUploads_UserId_UploadedAt");
        
        compositeIndex.Should().NotBeNull();
        compositeIndex!.Properties.Should().HaveCount(2);
    }

    [Fact]
    public void Configure_ShouldCreateIndexOnStoredFileName()
    {
        // Arrange
        var entityType = _context.Model.FindEntityType(typeof(VideoUpload));
        var indexes = entityType!.GetIndexes();

        // Assert
        var storedFileNameIndex = indexes.FirstOrDefault(i => 
            i.GetDatabaseName() == "IX_VideoUploads_StoredFileName");
        
        storedFileNameIndex.Should().NotBeNull();
    }
}
