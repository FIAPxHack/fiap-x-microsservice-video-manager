using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using VideoManagerService.Application.DTOs;
using VideoManagerService.Application.Interfaces;
using VideoManagerService.Application.UseCases;
using VideoManagerService.Domain.Entities;
using VideoManagerService.Domain.Enums;
using VideoManagerService.Domain.Interfaces.Repositories;
using VideoManagerService.Domain.Interfaces.Services;
using Xunit;

namespace VideoManagerService.Tests.Application.UseCases;

///  <summary>
/// Testes unitários para UploadVideoUseCase
/// </summary>
public class UploadVideoUseCaseTests
{
    private readonly Mock<IVideoRepository> _mockRepository;
    private readonly Mock<IFileStorageService> _mockStorageService;
    private readonly Mock<ILogger<UploadVideoUseCase>> _mockLogger;
    private readonly UploadVideoUseCase _useCase;

    public UploadVideoUseCaseTests()
    {
        _mockRepository = new Mock<IVideoRepository>();
        _mockStorageService = new Mock<IFileStorageService>();
        _mockLogger = new Mock<ILogger<UploadVideoUseCase>>();
        _useCase = new UploadVideoUseCase(
            _mockRepository.Object,
            _mockStorageService.Object,
            _mockLogger.Object);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidVideo_ShouldReturnSuccess()
    {
        // Arrange
        var mockFile = CreateMockFile("video.mp4", 1024);
        var request = new VideoUploadRequestDto
        {
            UserId = "user123",
            Email = "test@example.com",
            Video = mockFile.Object
        };

        _mockStorageService.Setup(x => x.SaveFileAsync(
            It.IsAny<Stream>(),
            It.IsAny<string>(),
            It.IsAny<string>()))
            .ReturnsAsync("/storage/videos/123/video.mp4");

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Should().NotBeNull();
        result.Success.Should().BeTrue();
        result.VideoId.Should().NotBeNullOrEmpty();
        result.Message.Should().Contain("Upload realizado com sucesso");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldSaveVideoToRepository()
    {
        // Arrange
        var mockFile = CreateMockFile("video.mp4", 1024);
        var request = new VideoUploadRequestDto
        {
            UserId = "user123",
            Email = "test@example.com",
            Video = mockFile.Object
        };

        _mockStorageService.Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("/path");

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        _mockRepository.Verify(x => x.SaveAsync(
            It.Is<VideoUpload>(v =>
                v.UserId == request.UserId &&
                v.OriginalFileName == "video.mp4" &&
                v.Status == ProcessingStatus.Pending)), Times.Once);
    }

    [Theory]
    [InlineData(".mp4")]
    [InlineData(".avi")]
    [InlineData(".mov")]
    [InlineData(".mkv")]
    [InlineData(".wmv")]
    public async Task ExecuteAsync_WithValidExtensions_ShouldAccept(string extension)
    {
        // Arrange
        var mockFile = CreateMockFile($"video{extension}", 1024);
        var request = new VideoUploadRequestDto
        {
            UserId = "user123",
            Email = "test@example.com",
            Video = mockFile.Object
        };

        _mockStorageService.Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("/path");

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeTrue();
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidExtension_ShouldReturnError()
    {
        // Arrange
        var mockFile = CreateMockFile("video.txt", 1024);
        var request = new VideoUploadRequestDto
        {
            UserId = "user123",
            Email = "test@example.com",
            Video = mockFile.Object
        };

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Formato de arquivo inválido");
    }

    [Fact]
    public async Task ExecuteAsync_WithFileTooLarge_ShouldReturnError()
    {
        // Arrange
        var mockFile = CreateMockFile("video.mp4", 600_000_000); // 600 MB > 500 MB limit
        var request = new VideoUploadRequestDto
        {
            UserId = "user123",
            Email = "test@example.com",
            Video = mockFile.Object
        };

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("tamanho máximo");
    }

    [Fact]
    public async Task ExecuteAsync_WhenStorageFails_ShouldReturnError()
    {
        // Arrange
        var mockFile = CreateMockFile("video.mp4", 1024);
        var request = new VideoUploadRequestDto
        {
            UserId = "user123",
            Email = "test@example.com",
            Video = mockFile.Object
        };

        _mockStorageService.Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new IOException("Disk full"));

        // Act
        var result = await _useCase.ExecuteAsync(request);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Erro interno");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldLogInformation()
    {
        // Arrange
        var mockFile = CreateMockFile("video.mp4", 1024);
        var request = new VideoUploadRequestDto
        {
            UserId = "user123",
            Email = "test@example.com",
            Video = mockFile.Object
        };

        _mockStorageService.Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("/path");

        // Act
        await _useCase.ExecuteAsync(request);

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Information,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Iniciando upload")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce);
    }

    private Mock<IFormFile> CreateMockFile(string fileName, long fileSize)
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(fileSize);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        mockFile.Setup(f => f.ContentType).Returns("video/mp4");
        return mockFile;
    }
}
