using FluentAssertions;
using VideoManagerService.Domain.Entities;
using VideoManagerService.Domain.Enums;
using Xunit;

namespace VideoManagerService.Tests.Domain.Entities;

/// <summary>
/// Testes unitários para a entidade VideoUpload
/// </summary>
public class VideoUploadTests
{
    [Fact]
    public void Constructor_ShouldCreateVideoUploadWithCorrectProperties()
    {
        // Arrange
        var userId = "user123";
        var originalFileName = "video.mp4";
        var storedFileName = "stored_video123.mp4";
        var filePath = "/storage/uploads/video123/video.mp4";
        var fileSizeBytes = 1024000L;

        // Act
        var video = new VideoUpload(userId, originalFileName, storedFileName, filePath, fileSizeBytes);

        // Assert
        video.UserId.Should().Be(userId);
        video.OriginalFileName.Should().Be(originalFileName);
        video.StoredFileName.Should().Be(storedFileName);
        video.FilePath.Should().Be(filePath);
        video.FileSizeBytes.Should().Be(fileSizeBytes);
        video.Status.Should().Be(ProcessingStatus.Pending);
        video.Id.Should().NotBeNullOrEmpty();
        video.UploadedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        video.FrameCount.Should().BeNull();
    }

    [Fact]
    public void Constructor_ShouldGenerateUniqueIds()
    {
        // Arrange & Act
        var video1 = new VideoUpload("user1", "video1.mp4", "stored1.mp4", "/path1", 1000);
        var video2 = new VideoUpload("user2", "video2.mp4", "stored2.mp4", "/path2", 2000);

        // Assert
        video1.Id.Should().NotBe(video2.Id);
    }

    [Fact]
    public void StartProcessing_ShouldUpdateStatusToProcessing()
    {
        // Arrange
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", "/path", 1000);
        var beforeStart = DateTime.UtcNow;

        // Act
        video.StartProcessing();

        // Assert
        video.Status.Should().Be(ProcessingStatus.Processing);
        video.ProcessingStartedAt.Should().NotBeNull();
        video.ProcessingStartedAt!.Value.Should().BeOnOrAfter(beforeStart);
        video.ProcessingCompletedAt.Should().BeNull();
        video.ZipFileName.Should().BeNull();
    }

    [Fact]
    public void CompleteProcessing_ShouldUpdateStatusAndSetCompletedAt()
    {
        // Arrange
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", "/path", 1000);
        video.StartProcessing();
        var beforeComplete = DateTime.UtcNow;

        // Act
        video.CompleteProcessing("frames.zip", 120);

        // Assert
        video.Status.Should().Be(ProcessingStatus.Completed);
        video.ZipFileName.Should().Be("frames.zip");
        video.FrameCount.Should().Be(120);
        video.ProcessingCompletedAt.Should().NotBeNull();
        video.ProcessingCompletedAt!.Value.Should().BeOnOrAfter(beforeComplete);
        video.ErrorMessage.Should().BeNull();
    }

    [Fact]
    public void MarkAsFailed_ShouldUpdateStatusAndSetErrorMessage()
    {
        // Arrange
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", "/path", 1000);
        video.StartProcessing();
        var errorMessage = "FFmpeg processing failed";

        // Act
        video.MarkAsFailed(errorMessage);

        // Assert
        video.Status.Should().Be(ProcessingStatus.Failed);
        video.ErrorMessage.Should().Be(errorMessage);
        video.ProcessingCompletedAt.Should().NotBeNull();
        video.ZipFileName.Should().BeNull();
    }

    [Theory]
    [InlineData(ProcessingStatus.Pending)]
    [InlineData(ProcessingStatus.Processing)]
    [InlineData(ProcessingStatus.Completed)]
    [InlineData(ProcessingStatus.Failed)]
    public void Status_ShouldAcceptAllProcessingStatuses(ProcessingStatus status)
    {
        // Arrange
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", "/path", 1000);

        // Act
        switch (status)
        {
            case ProcessingStatus.Processing:
                video.StartProcessing();
                break;
            case ProcessingStatus.Completed:
                video.StartProcessing();
                video.CompleteProcessing("test.zip", 10);
                break;
            case ProcessingStatus.Failed:
                video.StartProcessing();
                video.MarkAsFailed("error");
                break;
        }

        // Assert
        video.Status.Should().Be(status);
    }

    [Fact]
    public void StartProcessing_CalledTwice_ShouldThrowException()
    {
        // Arrange
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", "/path", 1000);
        video.StartProcessing();

        // Act & Assert
        var act = () => video.StartProcessing();
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*processamento*");
    }

    [Fact]
    public void CompleteProcessing_WithZeroFrames_ShouldStillWork()
    {
        // Arrange
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", "/path", 1000);
        video.StartProcessing();

        // Act
        video.CompleteProcessing("empty.zip", 0);

        // Assert
        video.Status.Should().Be(ProcessingStatus.Completed);
        video.FrameCount.Should().Be(0);
        video.ZipFileName.Should().Be("empty.zip");
    }

    [Fact]
    public void CompleteProcessing_WithLargeFrameCount_ShouldWork()
    {
        // Arrange
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", "/path", 1000);
        video.StartProcessing();

        // Act
        video.CompleteProcessing("frames.zip", 10000);

        // Assert
        video.FrameCount.Should().Be(10000);
        video.Status.Should().Be(ProcessingStatus.Completed);
    }

    [Fact]
    public void FileSize_ShouldStoreLargeValues()
    {
        // Arrange
        var largeFileSize = 500_000_000L; // 500 MB

        // Act
        var video = new VideoUpload("user", "large-video.mp4", "stored_large.mp4", "/path", largeFileSize);

        // Assert
        video.FileSizeBytes.Should().Be(largeFileSize);
    }

    [Fact]
    public void FilePath_ShouldStoreComplexPaths()
    {
        // Arrange
        var complexPath = "/storage/uploads/user-123-abc/video-456-def/original/video.mp4";

        // Act
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", complexPath, 1000);

        // Assert
        video.FilePath.Should().Be(complexPath);
    }

    [Fact]
    public void MarkAsFailed_WithEmptyErrorMessage_ShouldStillWork()
    {
        // Arrange
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", "/path", 1000);
        video.StartProcessing();

        // Act
        video.MarkAsFailed(string.Empty);

        // Assert
        video.Status.Should().Be(ProcessingStatus.Failed);
        video.ErrorMessage.Should().BeEmpty();
    }

    [Fact]
    public void MarkAsFailed_WithLongErrorMessage_ShouldStoreCompleteMessage()
    {
        // Arrange
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", "/path", 1000);
        video.StartProcessing();
        var longError = new string('E', 2000);

        // Act
        video.MarkAsFailed(longError);

        // Assert
        video.ErrorMessage.Should().Be(longError);
        video.ErrorMessage!.Length.Should().Be(2000);
    }

    [Fact]
    public void PendingVideo_ShouldNotHaveProcessingData()
    {
        // Arrange & Act
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", "/path", 1000);

        // Assert
        video.Status.Should().Be(ProcessingStatus.Pending);
        video.ProcessingStartedAt.Should().BeNull();
        video.ProcessingCompletedAt.Should().BeNull();
        video.ZipFileName.Should().BeNull();
        video.ErrorMessage.Should().BeNull();
        video.FrameCount.Should().BeNull();
    }

    [Fact]
    public void CompleteProcessing_BeforeStarting_ShouldThrowException()
    {
        // Arrange
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", "/path", 1000);

        // Act & Assert
        var act = () => video.CompleteProcessing("frames.zip", 50);
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*processamento*");
    }

    [Fact]
    public void ProcessingTimeline_ShouldBeConsistent()
    {
        // Arrange
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", "/path", 1000);
        var uploadedAt = video.UploadedAt;

        // Act
        video.StartProcessing();
        Thread.Sleep(10);
        video.CompleteProcessing("frames.zip", 100);

        // Assert
        video.UploadedAt.Should().BeBefore(video.ProcessingStartedAt!.Value);
        video.ProcessingStartedAt!.Value.Should().BeOnOrBefore(video.ProcessingCompletedAt!.Value);
    }

    [Theory]
    [InlineData("video.mp4", true)]
    [InlineData("video.avi", true)]
    [InlineData("video.mov", true)]
    [InlineData("video.mkv", true)]
    [InlineData("video.wmv", true)]
    [InlineData("video.flv", true)]
    [InlineData("video.webm", true)]
    [InlineData("VIDEO.MP4", true)]
    [InlineData("VIDEO.AVI", true)]
    [InlineData("document.pdf", false)]
    [InlineData("image.jpg", false)]
    [InlineData("file.txt", false)]
    [InlineData("file", false)]
    public void HasValidVideoExtension_ShouldValidateCorrectly(string fileName, bool expectedResult)
    {
        // Arrange
        var video = new VideoUpload("user", fileName, "stored.mp4", "/path", 1000);

        // Act
        var result = video.HasValidVideoExtension();

        // Assert
        result.Should().Be(expectedResult);
    }

    [Fact]
    public void GetProcessingDuration_ShouldReturnNull_WhenNotStarted()
    {
        // Arrange
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", "/path", 1000);

        // Act
        var duration = video.GetProcessingDuration();

        // Assert
        duration.Should().BeNull();
    }

    [Fact]
    public void GetProcessingDuration_ShouldReturnNull_WhenStartedButNotCompleted()
    {
        // Arrange
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", "/path", 1000);
        video.StartProcessing();

        // Act
        var duration = video.GetProcessingDuration();

        // Assert
        duration.Should().BeNull();
    }

    [Fact]
    public void GetProcessingDuration_ShouldReturnTimeSpan_WhenCompleted()
    {
        // Arrange
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", "/path", 1000);
        video.StartProcessing();
        Thread.Sleep(100); // Pequeno delay para garantir diferença de tempo
        video.CompleteProcessing("frames.zip", 100);

        // Act
        var duration = video.GetProcessingDuration();

        // Assert
        duration.Should().NotBeNull();
        duration!.Value.TotalMilliseconds.Should().BeGreaterThanOrEqualTo(0);
    }

    [Fact]
    public void GetProcessingDuration_ShouldReturnTimeSpan_WhenFailed()
    {
        // Arrange
        var video = new VideoUpload("user", "video.mp4", "stored.mp4", "/path", 1000);
        video.StartProcessing();
        Thread.Sleep(50);
        video.MarkAsFailed("Error occurred");

        // Act
        var duration = video.GetProcessingDuration();

        // Assert
        duration.Should().NotBeNull();
        duration!.Value.Should().BeGreaterThanOrEqualTo(TimeSpan.Zero);
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenUserIdIsNull()
    {
        // Act
        var act = () => new VideoUpload(null!, "video.mp4", "stored.mp4", "/path", 1000);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("userId");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenOriginalFileNameIsNull()
    {
        // Act
        var act = () => new VideoUpload("user", null!, "stored.mp4", "/path", 1000);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("originalFileName");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenStoredFileNameIsNull()
    {
        // Act
        var act = () => new VideoUpload("user", "video.mp4", null!, "/path", 1000);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("storedFileName");
    }

    [Fact]
    public void Constructor_ShouldThrowArgumentNullException_WhenFilePathIsNull()
    {
        // Act
        var act = () => new VideoUpload("user", "video.mp4", "stored.mp4", null!, 1000);

        // Assert
        act.Should().Throw<ArgumentNullException>().WithParameterName("filePath");
    }
}
