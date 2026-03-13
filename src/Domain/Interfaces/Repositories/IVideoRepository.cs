using VideoManagerService.Domain.Entities;

namespace VideoManagerService.Domain.Interfaces.Repositories;

/// <summary>
/// Interface do repositório de vídeos (contrato de domínio)
/// </summary>
public interface IVideoRepository
{
    /// <summary>
    /// Salva um novo upload de vídeo
    /// </summary>
    Task<VideoUpload> SaveAsync(VideoUpload video);
    
    /// <summary>
    /// Busca um vídeo por ID
    /// </summary>
    Task<VideoUpload?> GetByIdAsync(string id);
    
    /// <summary>
    /// Busca todos os vídeos de um usuário
    /// </summary>
    Task<IEnumerable<VideoUpload>> GetByUserIdAsync(string userId);
    
    /// <summary>
    /// Atualiza um vídeo existente
    /// </summary>
    Task UpdateAsync(VideoUpload video);
    
    /// <summary>
    /// Deleta um vídeo
    /// </summary>
    Task DeleteAsync(string id);
    
    /// <summary>
    /// Busca todos os vídeos com status específico
    /// </summary>
    Task<IEnumerable<VideoUpload>> GetByStatusAsync(VideoManagerService.Domain.Enums.ProcessingStatus status);
}
