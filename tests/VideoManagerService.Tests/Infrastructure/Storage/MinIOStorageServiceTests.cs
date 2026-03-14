using System.Net;
using Amazon.S3;
using Amazon.S3.Model;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using VideoManagerService.Infrastructure.Storage;

namespace VideoManagerService.Tests.Infrastructure.Storage;

public class MinIOStorageServiceTests
{
    private readonly Mock<IAmazonS3> _s3ClientMock;
    private readonly Mock<ILogger<MinIOStorageService>> _loggerMock;
    private readonly Mock<IConfiguration> _configurationMock;
    private readonly string _bucketName = "test-bucket";

    public MinIOStorageServiceTests()
    {
        _s3ClientMock = new Mock<IAmazonS3>();
        _loggerMock = new Mock<ILogger<MinIOStorageService>>();
        _configurationMock = new Mock<IConfiguration>();

        _configurationMock.Setup(c => c["MinIO:BucketName"])
            .Returns(_bucketName);

        // Mock bucket existence check
        _s3ClientMock.Setup(x => x.ListBucketsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ListBucketsResponse
            {
                Buckets = new List<Amazon.S3.Model.S3Bucket>
                {
                    new Amazon.S3.Model.S3Bucket { BucketName = _bucketName }
                }
            });
    }

    [Fact]
    public void Constructor_ShouldThrowInvalidOperationException_WhenBucketNameConfigurationIsMissing()
    {
        // Arrange
        var configMock = new Mock<IConfiguration>();
        configMock.Setup(c => c["MinIO:BucketName"]).Returns((string?)null);

        // Act
        Action act = () => new MinIOStorageService(_s3ClientMock.Object, configMock.Object, _loggerMock.Object);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("MinIO:BucketName configuration is missing");
    }

    [Fact]
    public async Task SaveFileAsync_ShouldSaveFileWithCorrectKey_WhenDirectoryIsProvided()
    {
        // Arrange
        var service = CreateService();
        var fileName = "test-video.mp4";
        var directory = "videos";
        var expectedKey = "videos/test-video.mp4";
        PutObjectRequest? capturedRequest = null;

        _s3ClientMock.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        using var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        // Act
        var result = await service.SaveFileAsync(fileStream, fileName, directory);

        // Assert
        result.Should().Be(expectedKey);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.BucketName.Should().Be(_bucketName);
        capturedRequest.Key.Should().Be(expectedKey);
        capturedRequest.ContentType.Should().Be("video/mp4");
    }

    [Fact]
    public async Task SaveFileAsync_ShouldSaveFileWithFileNameAsKey_WhenDirectoryIsEmpty()
    {
        // Arrange
        var service = CreateService();
        var fileName = "test-video.mp4";
        PutObjectRequest? capturedRequest = null;

        _s3ClientMock.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        using var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        // Act
        var result = await service.SaveFileAsync(fileStream, fileName, "");

        // Assert
        result.Should().Be(fileName);
        capturedRequest.Should().NotBeNull();
        capturedRequest!.Key.Should().Be(fileName);
    }

    [Theory]
    [InlineData("video.mp4", "video/mp4")]
    [InlineData("video.avi", "video/x-msvideo")]
    [InlineData("video.mov", "video/quicktime")]
    [InlineData("video.mkv", "video/x-matroska")]
    [InlineData("file.zip", "application/zip")]
    [InlineData("image.png", "image/png")]
    [InlineData("image.jpg", "image/jpeg")]
    [InlineData("image.jpeg", "image/jpeg")]
    [InlineData("unknown.xyz", "application/octet-stream")]
    public async Task SaveFileAsync_ShouldSetCorrectContentType_ForDifferentFileTypes(string fileName, string expectedContentType)
    {
        // Arrange
        var service = CreateService();
        PutObjectRequest? capturedRequest = null;

        _s3ClientMock.Setup(x => x.PutObjectAsync(It.IsAny<PutObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<PutObjectRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new PutObjectResponse { HttpStatusCode = HttpStatusCode.OK });

        using var fileStream = new MemoryStream(new byte[] { 1, 2, 3, 4 });

        // Act
        await service.SaveFileAsync(fileStream, fileName, "");

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.ContentType.Should().Be(expectedContentType);
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldDeleteFile_WhenCalled()
    {
        // Arrange
        var service = CreateService();
        var filePath = "videos/test-video.mp4";
        DeleteObjectRequest? capturedRequest = null;

        _s3ClientMock.Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<DeleteObjectRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new DeleteObjectResponse { HttpStatusCode = HttpStatusCode.NoContent });

        // Act
        await service.DeleteFileAsync(filePath);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.BucketName.Should().Be(_bucketName);
        capturedRequest.Key.Should().Be(filePath);
    }

    [Fact]
    public async Task DeleteFileAsync_ShouldLogWarning_WhenExceptionOccurs()
    {
        // Arrange
        var service = CreateService();
        var filePath = "videos/test-video.mp4";

        _s3ClientMock.Setup(x => x.DeleteObjectAsync(It.IsAny<DeleteObjectRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Error deleting file"));

        // Act
        await service.DeleteFileAsync(filePath);

        // Assert - não deve lançar exceção, apenas logar warning
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((o, t) => true),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task FileExistsAsync_ShouldReturnTrue_WhenFileExists()
    {
        // Arrange
        var service = CreateService();
        var filePath = "videos/test-video.mp4";

        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectMetadataResponse { HttpStatusCode = HttpStatusCode.OK });

        // Act
        var result = await service.FileExistsAsync(filePath);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task FileExistsAsync_ShouldReturnFalse_WhenFileDoesNotExist()
    {
        // Arrange
        var service = CreateService();
        var filePath = "videos/non-existent.mp4";

        _s3ClientMock.Setup(x => x.GetObjectMetadataAsync(It.IsAny<GetObjectMetadataRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AmazonS3Exception("Not found") { StatusCode = HttpStatusCode.NotFound });

        // Act
        var result = await service.FileExistsAsync(filePath);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetFullPath_ShouldReturnRelativePath_WhenCalled()
    {
        // Arrange
        var service = CreateService();
        var relativePath = "videos/test-video.mp4";

        // Act
        var result = service.GetFullPath(relativePath);

        // Assert
        result.Should().Be(relativePath);
    }

    [Fact]
    public async Task GetFileStreamAsync_ShouldReturnFileStream_WhenFileExists()
    {
        // Arrange
        var service = CreateService();
        var filePath = "videos/test-video.mp4";
        var fileContent = new byte[] { 1, 2, 3, 4, 5 };
        var responseStream = new MemoryStream(fileContent);

        _s3ClientMock.Setup(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new GetObjectResponse 
            { 
                ResponseStream = responseStream,
                HttpStatusCode = HttpStatusCode.OK
            });

        // Act
        var result = await service.GetFileStreamAsync(filePath);

        // Assert
        result.Should().NotBeNull();
        result.Length.Should().Be(fileContent.Length);
        
        var buffer = new byte[fileContent.Length];
        await result.ReadAsync(buffer, 0, buffer.Length);
        buffer.Should().Equal(fileContent);
        
        result.Dispose();
    }

    [Fact]
    public async Task GetFileStreamAsync_ShouldRequestCorrectFile_WhenCalled()
    {
        // Arrange
        var service = CreateService();
        var filePath = "videos/test-video.mp4";
        GetObjectRequest? capturedRequest = null;

        _s3ClientMock.Setup(x => x.GetObjectAsync(It.IsAny<GetObjectRequest>(), It.IsAny<CancellationToken>()))
            .Callback<GetObjectRequest, CancellationToken>((req, _) => capturedRequest = req)
            .ReturnsAsync(new GetObjectResponse 
            { 
                ResponseStream = new MemoryStream(new byte[] { 1, 2, 3 }),
                HttpStatusCode = HttpStatusCode.OK
            });

        // Act
        var result = await service.GetFileStreamAsync(filePath);

        // Assert
        capturedRequest.Should().NotBeNull();
        capturedRequest!.BucketName.Should().Be(_bucketName);
        capturedRequest.Key.Should().Be(filePath);
        
        result.Dispose();
    }

    private MinIOStorageService CreateService()
    {
        return new MinIOStorageService(_s3ClientMock.Object, _configurationMock.Object, _loggerMock.Object);
    }
}
