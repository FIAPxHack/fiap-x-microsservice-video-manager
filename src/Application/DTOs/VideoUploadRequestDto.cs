using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace VideoManagerService.Application.DTOs;

/// <summary>
/// DTO para upload de vídeo
/// </summary>
public class VideoUploadRequestDto
{
    /// <summary>
    /// ID do usuário que está fazendo o upload
    /// </summary>
    [Required(ErrorMessage = "UserId é obrigatório")]
    public string UserId { get; set; } = string.Empty;
    
    /// <summary>
    /// Email do usuário para notificações
    /// </summary>
    [Required(ErrorMessage = "Email é obrigatório")]
    [EmailAddress(ErrorMessage = "Email em formato inválido")]
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// Arquivo de vídeo
    /// </summary>
    [Required(ErrorMessage = "Arquivo de vídeo é obrigatório")]
    public IFormFile Video { get; set; } = null!;
}
