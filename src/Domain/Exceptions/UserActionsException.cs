namespace VideoManagerService.Domain.Exceptions;

/// <summary>
/// Exceção de domínio base para o serviço de ações do usuário
/// </summary>
public class UserActionsException : Exception
{
    public UserActionsException(string message) : base(message)
    {
    }

    public UserActionsException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exceção lançada quando o formato do arquivo é inválido
/// </summary>
public class InvalidFileFormatException : UserActionsException
{
    public InvalidFileFormatException(string fileName)
        : base($"Formato de arquivo inválido: {fileName}. Somente vídeos são permitidos.")
    {
    }
}

/// <summary>
/// Exceção lançada quando o vídeo não é encontrado
/// </summary>
public class VideoNotFoundException : UserActionsException
{
    public VideoNotFoundException(string videoId)
        : base($"Vídeo não encontrado: {videoId}")
    {
    }
}

/// <summary>
/// Exceção lançada quando ocorre erro no processamento
/// </summary>
public class VideoProcessingException : UserActionsException
{
    public VideoProcessingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>
/// Exceção lançada quando o arquivo excede o tamanho máximo
/// </summary>
public class FileSizeExceededException : UserActionsException
{
    public FileSizeExceededException(long maxSizeBytes)
        : base($"Arquivo excede o tamanho máximo permitido de {maxSizeBytes / 1024 / 1024} MB")
    {
    }
}
