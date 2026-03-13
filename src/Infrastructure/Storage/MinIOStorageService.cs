using Amazon.S3;
using Amazon.S3.Model;
using VideoManagerService.Domain.Interfaces.Services;

namespace VideoManagerService.Infrastructure.Storage;

public class MinIOStorageService : IFileStorageService
{
    private readonly IAmazonS3 _s3Client;
    private readonly string _bucketName;
    private readonly ILogger<MinIOStorageService> _logger;

    public MinIOStorageService(
        IAmazonS3 s3Client,
        IConfiguration configuration,
        ILogger<MinIOStorageService> logger)
    {
        _s3Client = s3Client;
        _bucketName = configuration["MinIO:BucketName"] 
            ?? throw new InvalidOperationException("MinIO:BucketName configuration is missing");
        _logger = logger;

        EnsureBucketExistsAsync().GetAwaiter().GetResult();
    }

    public async Task<string> SaveFileAsync(Stream fileStream, string fileName, string directory)
    {
        var key = string.IsNullOrWhiteSpace(directory) 
            ? fileName 
            : $"{directory}/{fileName}";

        var putRequest = new PutObjectRequest
        {
            BucketName = _bucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = GetContentType(fileName)
        };

        await _s3Client.PutObjectAsync(putRequest);

        _logger.LogInformation("Arquivo salvo no MinIO: {Key}", key);

        return key;
    }

    public async Task DeleteFileAsync(string filePath)
    {
        try
        {
            var deleteRequest = new DeleteObjectRequest
            {
                BucketName = _bucketName,
                Key = filePath
            };

            await _s3Client.DeleteObjectAsync(deleteRequest);
            _logger.LogInformation("Arquivo deletado do MinIO: {FilePath}", filePath);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Erro ao deletar arquivo do MinIO: {FilePath}", filePath);
        }
    }

    public async Task<bool> FileExistsAsync(string filePath)
    {
        try
        {
            var request = new GetObjectMetadataRequest
            {
                BucketName = _bucketName,
                Key = filePath
            };

            await _s3Client.GetObjectMetadataAsync(request);
            return true;
        }
        catch (AmazonS3Exception ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            return false;
        }
    }

    public string GetFullPath(string relativePath)
    {
        return relativePath;
    }

    public async Task<Stream> GetFileStreamAsync(string filePath)
    {
        var request = new GetObjectRequest
        {
            BucketName = _bucketName,
            Key = filePath
        };

        var response = await _s3Client.GetObjectAsync(request);
        
        var memoryStream = new MemoryStream();
        await response.ResponseStream.CopyToAsync(memoryStream);
        memoryStream.Position = 0;

        return memoryStream;
    }

    private async Task EnsureBucketExistsAsync()
    {
        try
        {
            var bucketExists = await Amazon.S3.Util.AmazonS3Util.DoesS3BucketExistV2Async(_s3Client, _bucketName);

            if (!bucketExists)
            {
                await _s3Client.PutBucketAsync(new PutBucketRequest
                {
                    BucketName = _bucketName,
                    UseClientRegion = true
                });

                _logger.LogInformation("Bucket criado: {BucketName}", _bucketName);
            }
            else
            {
                _logger.LogInformation("Bucket já existe: {BucketName}", _bucketName);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Erro ao verificar/criar bucket {BucketName}", _bucketName);
            throw;
        }
    }

    private static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        return extension switch
        {
            ".mp4" => "video/mp4",
            ".avi" => "video/x-msvideo",
            ".mov" => "video/quicktime",
            ".mkv" => "video/x-matroska",
            ".zip" => "application/zip",
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            _ => "application/octet-stream"
        };
    }
}
