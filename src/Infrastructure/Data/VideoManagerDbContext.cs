using Microsoft.EntityFrameworkCore;
using VideoManagerService.Domain.Entities;

namespace VideoManagerService.Infrastructure.Data;

/// <summary>
/// Contexto do Entity Framework Core para o User Actions Service
/// </summary>
public class VideoManagerDbContext : DbContext
{
    public DbSet<VideoUpload> VideoUploads { get; set; } = null!;

    public VideoManagerDbContext(DbContextOptions<VideoManagerDbContext> options)
        : base(options)
    {
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Aplicar todas as configurações de entidades do assembly
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(VideoManagerDbContext).Assembly);
    }
}
