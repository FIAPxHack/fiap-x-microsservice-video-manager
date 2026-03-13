using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VideoManagerService.Domain.Entities;
using VideoManagerService.Domain.Enums;

namespace VideoManagerService.Infrastructure.Data.Configurations;

/// <summary>
/// Configuração de mapeamento da entidade VideoUpload para o banco de dados
/// </summary>
public class VideoUploadConfiguration : IEntityTypeConfiguration<VideoUpload>
{
    public void Configure(EntityTypeBuilder<VideoUpload> builder)
    {
        // Nome da tabela
        builder.ToTable("video_uploads");

        // Chave primária
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id)
            .HasMaxLength(50)
            .IsRequired();

        // Propriedades obrigatórias
        builder.Property(v => v.UserId)
            .HasMaxLength(100)
            .IsRequired();

        builder.Property(v => v.OriginalFileName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(v => v.StoredFileName)
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(v => v.FilePath)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(v => v.FileSizeBytes)
            .IsRequired();

        // Enum como string no banco
        builder.Property(v => v.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        // Timestamps
        builder.Property(v => v.UploadedAt)
            .IsRequired();

        builder.Property(v => v.ProcessingStartedAt)
            .IsRequired(false);

        builder.Property(v => v.ProcessingCompletedAt)
            .IsRequired(false);

        // Propriedades opcionais
        builder.Property(v => v.ZipFileName)
            .HasMaxLength(500)
            .IsRequired(false);

        builder.Property(v => v.FrameCount)
            .IsRequired(false);

        builder.Property(v => v.ErrorMessage)
            .HasMaxLength(2000)
            .IsRequired(false);

        // Índices para performance
        builder.HasIndex(v => v.UserId)
            .HasDatabaseName("IX_VideoUploads_UserId");

        builder.HasIndex(v => v.Status)
            .HasDatabaseName("IX_VideoUploads_Status");

        builder.HasIndex(v => new { v.UserId, v.UploadedAt })
            .HasDatabaseName("IX_VideoUploads_UserId_UploadedAt");

        builder.HasIndex(v => v.StoredFileName)
            .HasDatabaseName("IX_VideoUploads_StoredFileName");
    }
}
