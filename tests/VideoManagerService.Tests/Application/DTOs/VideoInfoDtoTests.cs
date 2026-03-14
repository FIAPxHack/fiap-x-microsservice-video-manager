using FluentAssertions;
using VideoManagerService.Application.DTOs;
using VideoManagerService.Domain.Enums;

namespace VideoManagerService.Tests.Application.DTOs;

public class VideoInfoDtoTests
{
    [Fact]
    public void FileSizeMB_ShouldFormatCorrectly()
    {
        // Arrange
        var dto = new VideoInfoDto
        {
            FileSizeBytes = 1048576 // 1 MB
        };

        // Act & Assert
        dto.FileSizeMB.Should().Be("1.00 MB");
    }

    [Theory]
    [InlineData(0, "0.00 MB")]
    [InlineData(512000, "0.49 MB")]
    [InlineData(1048576, "1.00 MB")]
    [InlineData(10485760, "10.00 MB")]
    [InlineData(104857600, "100.00 MB")]
    [InlineData(1073741824, "1024.00 MB")]
    public void FileSizeMB_ShouldCalculateCorrectly_ForDifferentSizes(long bytes, string expected)
    {
        // Arrange
        var dto = new VideoInfoDto { FileSizeBytes = bytes };

        // Act & Assert
        dto.FileSizeMB.Should().Be(expected);
    }

    [Theory]
    [InlineData(ProcessingStatus.Pending, "Aguardando processamento")]
    [InlineData(ProcessingStatus.Processing, "Em processamento")]
    [InlineData(ProcessingStatus.Completed, "Processamento concluído")]
    [InlineData(ProcessingStatus.Failed, "Falha no processamento")]
    public void StatusDescription_ShouldReturnCorrectDescription(ProcessingStatus status, string expected)
    {
        // Arrange
        var dto = new VideoInfoDto { Status = status };

        // Act & Assert
        dto.StatusDescription.Should().Be(expected);
    }

    [Fact]
    public void StatusDescription_ShouldReturnUnknown_ForInvalidStatus()
    {
        // Arrange
        var dto = new VideoInfoDto { Status = (ProcessingStatus)999 };

        // Act & Assert
        dto.StatusDescription.Should().Be("Status desconhecido");
    }

    [Fact]
    public void Properties_ShouldBeSettableAndGettable()
    {
        // Arrange
        var id = "video-123";
        var userId = "user-456";
        var fileName = "test-video.mp4";
        var fileSize = 5242880L;
        var uploadedAt = DateTime.UtcNow;
        var processingStartedAt = DateTime.UtcNow.AddMinutes(1);
        var processingCompletedAt = DateTime.UtcNow.AddMinutes(5);
        var zipFileName = "frames.zip";
        var frameCount = 120;
        var errorMessage = "Some error";
        var processingDuration = "00:04:00";

        // Act
        var dto = new VideoInfoDto
        {
            Id = id,
            UserId = userId,
            OriginalFileName = fileName,
            FileSizeBytes = fileSize,
            Status = ProcessingStatus.Completed,
            UploadedAt = uploadedAt,
            ProcessingStartedAt = processingStartedAt,
            ProcessingCompletedAt = processingCompletedAt,
            ZipFileName = zipFileName,
            FrameCount = frameCount,
            ErrorMessage = errorMessage,
            ProcessingDuration = processingDuration
        };

        // Assert
        dto.Id.Should().Be(id);
        dto.UserId.Should().Be(userId);
        dto.OriginalFileName.Should().Be(fileName);
        dto.FileSizeBytes.Should().Be(fileSize);
        dto.Status.Should().Be(ProcessingStatus.Completed);
        dto.UploadedAt.Should().Be(uploadedAt);
        dto.ProcessingStartedAt.Should().Be(processingStartedAt);
        dto.ProcessingCompletedAt.Should().Be(processingCompletedAt);
        dto.ZipFileName.Should().Be(zipFileName);
        dto.FrameCount.Should().Be(frameCount);
        dto.ErrorMessage.Should().Be(errorMessage);
        dto.ProcessingDuration.Should().Be(processingDuration);
    }

    [Fact]
    public void OptionalProperties_ShouldBeNullableAndSettable()
    {
        // Arrange & Act
        var dto = new VideoInfoDto
        {
            ProcessingStartedAt = null,
            ProcessingCompletedAt = null,
            ZipFileName = null,
            FrameCount = null,
            ErrorMessage = null,
            ProcessingDuration = null
        };

        // Assert
        dto.ProcessingStartedAt.Should().BeNull();
        dto.ProcessingCompletedAt.Should().BeNull();
        dto.ZipFileName.Should().BeNull();
        dto.FrameCount.Should().BeNull();
        dto.ErrorMessage.Should().BeNull();
        dto.ProcessingDuration.Should().BeNull();
    }

    [Fact]
    public void DefaultValues_ShouldBeEmpty()
    {
        // Act
        var dto = new VideoInfoDto();

        // Assert
        dto.Id.Should().BeEmpty();
        dto.UserId.Should().BeEmpty();
        dto.OriginalFileName.Should().BeEmpty();
        dto.FileSizeBytes.Should().Be(0);
        dto.Status.Should().Be(ProcessingStatus.Pending);
    }
}
