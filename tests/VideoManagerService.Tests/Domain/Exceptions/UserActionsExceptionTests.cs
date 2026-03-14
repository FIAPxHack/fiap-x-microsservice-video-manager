using FluentAssertions;
using VideoManagerService.Domain.Exceptions;
using Xunit;

namespace VideoManagerService.Tests.Domain.Exceptions;

public class UserActionsExceptionTests
{
    [Fact]
    public void UserActionsException_WithMessage_ShouldSetMessage()
    {
        // Arrange
        var message = "Test exception message";

        // Act
        var exception = new UserActionsException(message);

        // Assert
        exception.Message.Should().Be(message);
    }

    [Fact]
    public void UserActionsException_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        var message = "Test exception message";
        var innerException = new InvalidOperationException("Inner exception");

        // Act
        var exception = new UserActionsException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
    }

    [Fact]
    public void InvalidFileFormatException_WithFileName_ShouldContainFileName()
    {
        // Arrange
        var fileName = "invalid.txt";

        // Act
        var exception = new InvalidFileFormatException(fileName);

        // Assert
        exception.Message.Should().Contain(fileName);
        exception.Message.Should().Contain("Formato de arquivo inválido");
        exception.Should().BeAssignableTo<UserActionsException>();
    }

    [Fact]
    public void VideoNotFoundException_WithVideoId_ShouldContainVideoId()
    {
        // Arrange
        var videoId = "video-123";

        // Act
        var exception = new VideoNotFoundException(videoId);

        // Assert
        exception.Message.Should().Contain(videoId);
        exception.Message.Should().Contain("Vídeo não encontrado");
        exception.Should().BeAssignableTo<UserActionsException>();
    }

    [Fact]
    public void VideoProcessingException_WithMessageAndInnerException_ShouldSetBoth()
    {
        // Arrange
        var message = "Processing failed";
        var innerException = new Exception("Inner exception");

        // Act
        var exception = new VideoProcessingException(message, innerException);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().Be(innerException);
        exception.Should().BeAssignableTo<UserActionsException>();
    }

    [Fact]
    public void FileSizeExceededException_WithMaxSize_ShouldContainMaxSize()
    {
        // Arrange
        long maxSizeBytes = 10485760; // 10 MB

        // Act
        var exception = new FileSizeExceededException(maxSizeBytes);

        // Assert
        exception.Message.Should().Contain("10 MB");
        exception.Message.Should().Contain("tamanho máximo");
        exception.Should().BeAssignableTo<UserActionsException>();
    }
}
