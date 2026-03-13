namespace VideoManagerService.Domain.Interfaces.Services;

public interface IFileStorageService
{
    Task<string> SaveFileAsync(Stream fileStream, string fileName, string directory);
    
    Task DeleteFileAsync(string filePath);
    
    Task<bool> FileExistsAsync(string filePath);
    
    string GetFullPath(string relativePath);
    
    Task<Stream> GetFileStreamAsync(string filePath);
}
