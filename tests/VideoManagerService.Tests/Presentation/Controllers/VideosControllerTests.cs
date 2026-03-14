using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using VideoManagerService.Application.DTOs;
using VideoManagerService.Application.Interfaces;
using VideoManagerService.Domain.Enums;
using VideoManagerService.Domain.Interfaces.Services;
using VideoManagerService.Presentation.Controllers;
using Xunit;

namespace VideoManagerService.Tests.Presentation.Controllers;

public class VideosControllerTests
{
    private readonly Mock<IUploadVideoUseCase> _mockUploadUseCase;
    private readonly Mock<IGetUserVideosUseCase> _mockGetUserVideosUseCase;
    private readonly Mock<IGetVideoStatusUseCase> _mockGetVideoStatusUseCase;
    private readonly Mock<IUpdateVideoStatusUseCase> _mockUpdateVideoStatusUseCase;
    private readonly Mock<IFileStorageService> _mockFileStorageService;
    private readonly Mock<ILogger<VideosController>> _mockLogger;
    private readonly VideosController _controller;

    public VideosControllerTests()
    {
        _mockUploadUseCase = new Mock<IUploadVideoUseCase>();
        _mockGetUserVideosUseCase = new Mock<IGetUserVideosUseCase>();
        _mockGetVideoStatusUseCase = new Mock<IGetVideoStatusUseCase>();
        _mockUpdateVideoStatusUseCase = new Mock<IUpdateVideoStatusUseCase>();
        _mockFileStorageService = new Mock<IFileStorageService>();
        _mockLogger = new Mock<ILogger<VideosController>>();

        _controller = new VideosController(
            _mockUploadUseCase.Object,
            _mockGetUserVideosUseCase.Object,
            _mockGetVideoStatusUseCase.Object,
            _mockUpdateVideoStatusUseCase.Object,
            _mockFileStorageService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task UploadVideo_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var mockFile = CreateMockFile("video.mp4", 1024);
        var request = new VideoUploadRequestDto
        {
            UserId = "user-123",
            Email = "test@example.com",
            Video = mockFile.Object
        };

        var response = new VideoUploadResponseDto
        {
            Success = true,
            VideoId = "video-123",
            Message = "Upload realizado com sucesso"
        };

        _mockUploadUseCase.Setup(x => x.ExecuteAsync(It.IsAny<VideoUploadRequestDto>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.UploadVideo(request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(response);
    }

    [Fact]
    public async Task UploadVideo_WithFailedUpload_ShouldReturnBadRequest()
    {
        // Arrange
        var mockFile = CreateMockFile("video.mp4", 1024);
        var request = new VideoUploadRequestDto
        {
            UserId = "user-123",
            Email = "test@example.com",
            Video = mockFile.Object
        };

        var response = new VideoUploadResponseDto
        {
            Success = false,
            Message = "Upload failed"
        };

        _mockUploadUseCase.Setup(x => x.ExecuteAsync(It.IsAny<VideoUploadRequestDto>()))
            .ReturnsAsync(response);

        // Act
        var result = await _controller.UploadVideo(request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetUserVideos_WithValidUserId_ShouldReturnOk()
    {
        // Arrange
        var userId = "user-123";
        var videos = new List<VideoInfoDto>
        {
            new VideoInfoDto
            {
                Id = "video-1",
                UserId = userId,
                OriginalFileName = "video1.mp4",
                Status = ProcessingStatus.Completed
            },
            new VideoInfoDto
            {
                Id = "video-2",
                UserId = userId,
                OriginalFileName = "video2.mp4",
                Status = ProcessingStatus.Processing
            }
        };

        _mockGetUserVideosUseCase.Setup(x => x.ExecuteAsync(userId))
            .ReturnsAsync(videos);

        // Act
        var result = await _controller.GetUserVideos(userId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(videos);
    }

    [Fact]
    public async Task GetUserVideos_WithEmptyUserId_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.GetUserVideos("");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task GetVideoStatus_WithValidVideoId_ShouldReturnOk()
    {
        // Arrange
        var videoId = "video-123";
        var videoInfo = new VideoInfoDto
        {
            Id = videoId,
            UserId = "user-123",
            OriginalFileName = "video.mp4",
            Status = ProcessingStatus.Completed
        };

        _mockGetVideoStatusUseCase.Setup(x => x.ExecuteAsync(videoId))
            .ReturnsAsync(videoInfo);

        // Act
        var result = await _controller.GetVideoStatus(videoId);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(videoInfo);
    }

    [Fact]
    public async Task GetVideoStatus_WithNonExistentVideo_ShouldReturnNotFound()
    {
        // Arrange
        var videoId = "non-existent";
        _mockGetVideoStatusUseCase.Setup(x => x.ExecuteAsync(videoId))
            .ReturnsAsync((VideoInfoDto?)null);

        // Act
        var result = await _controller.GetVideoStatus(videoId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task GetVideoStatus_WithEmptyVideoId_ShouldReturnBadRequest()
    {
        // Act
        var result = await _controller.GetVideoStatus("");

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DownloadFrames_WithCompletedVideo_ShouldReturnFile()
    {
        // Arrange
        var videoId = "video-123";
        var videoInfo = new VideoInfoDto
        {
            Id = videoId,
            Status = ProcessingStatus.Completed,
            ZipFileName = "output.zip"
        };

        var fileStream = new MemoryStream(new byte[100]);
        
        _mockGetVideoStatusUseCase.Setup(x => x.ExecuteAsync(videoId))
            .ReturnsAsync(videoInfo);
        _mockFileStorageService.Setup(x => x.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(true);
        _mockFileStorageService.Setup(x => x.GetFileStreamAsync(It.IsAny<string>()))
            .ReturnsAsync(fileStream);

        // Act
        var result = await _controller.DownloadFrames(videoId);

        // Assert
        result.Should().BeOfType<FileStreamResult>();
        var fileResult = result as FileStreamResult;
        fileResult!.FileDownloadName.Should().Be("output.zip");
    }

    [Fact]
    public async Task DownloadFrames_WithNonExistentVideo_ShouldReturnNotFound()
    {
        // Arrange
        var videoId = "non-existent";
        _mockGetVideoStatusUseCase.Setup(x => x.ExecuteAsync(videoId))
            .ReturnsAsync((VideoInfoDto?)null);

        // Act
        var result = await _controller.DownloadFrames(videoId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task DownloadFrames_WithNonCompletedVideo_ShouldReturnBadRequest()
    {
        // Arrange
        var videoId = "video-123";
        var videoInfo = new VideoInfoDto
        {
            Id = videoId,
            Status = ProcessingStatus.Processing,
            ZipFileName = null
        };

        _mockGetVideoStatusUseCase.Setup(x => x.ExecuteAsync(videoId))
            .ReturnsAsync(videoInfo);

        // Act
        var result = await _controller.DownloadFrames(videoId);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task DownloadFrames_WithMissingFileInStorage_ShouldReturnNotFound()
    {
        // Arrange
        var videoId = "video-123";
        var videoInfo = new VideoInfoDto
        {
            Id = videoId,
            Status = ProcessingStatus.Completed,
            ZipFileName = "output.zip"
        };

        _mockGetVideoStatusUseCase.Setup(x => x.ExecuteAsync(videoId))
            .ReturnsAsync(videoInfo);
        _mockFileStorageService.Setup(x => x.FileExistsAsync(It.IsAny<string>()))
            .ReturnsAsync(false);

        // Act
        var result = await _controller.DownloadFrames(videoId);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateVideoStatus_WithValidRequest_ShouldReturnOk()
    {
        // Arrange
        var videoId = "video-123";
        var request = new UpdateVideoStatusRequestDto
        {
            Status = ProcessingStatus.Completed,
            ZipFileName = "output.zip",
            FrameCount = 100
        };

        var updatedVideo = new VideoInfoDto
        {
            Id = videoId,
            Status = ProcessingStatus.Completed,
            ZipFileName = "output.zip",
            FrameCount = 100
        };

        _mockUpdateVideoStatusUseCase.Setup(x => x.ExecuteAsync(videoId, request))
            .ReturnsAsync(updatedVideo);

        // Act
        var result = await _controller.UpdateVideoStatus(videoId, request);

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        var okResult = result as OkObjectResult;
        okResult!.Value.Should().BeEquivalentTo(updatedVideo);
    }

    [Fact]
    public async Task UpdateVideoStatus_WithNonExistentVideo_ShouldReturnNotFound()
    {
        // Arrange
        var videoId = "non-existent";
        var request = new UpdateVideoStatusRequestDto
        {
            Status = ProcessingStatus.Processing
        };

        _mockUpdateVideoStatusUseCase.Setup(x => x.ExecuteAsync(videoId, request))
            .ReturnsAsync((VideoInfoDto?)null);

        // Act
        var result = await _controller.UpdateVideoStatus(videoId, request);

        // Assert
        result.Should().BeOfType<NotFoundObjectResult>();
    }

    [Fact]
    public async Task UpdateVideoStatus_WithEmptyVideoId_ShouldReturnBadRequest()
    {
        // Arrange
        var request = new UpdateVideoStatusRequestDto
        {
            Status = ProcessingStatus.Processing
        };

        // Act
        var result = await _controller.UpdateVideoStatus("", request);

        // Assert
        result.Should().BeOfType<BadRequestObjectResult>();
    }

    private static Mock<IFormFile> CreateMockFile(string fileName, long size)
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(size);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream(new byte[size]));
        mockFile.Setup(f => f.ContentType).Returns("video/mp4");
        return mockFile;
    }
}
