using Amazon.S3;
using Amazon.Runtime;
using Microsoft.EntityFrameworkCore;
using Prometheus;
using VideoManagerService.Application.Interfaces;
using VideoManagerService.Application.UseCases;
using VideoManagerService.Domain.Interfaces.Repositories;
using VideoManagerService.Domain.Interfaces.Services;
using VideoManagerService.Infrastructure.Data;
using VideoManagerService.Infrastructure.ExternalServices;
using VideoManagerService.Infrastructure.Persistence;
using VideoManagerService.Infrastructure.Storage;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddApplicationPart(typeof(VideoManagerService.Presentation.Controllers.VideosController).Assembly);

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "FIAP-x - Video Manager Service API",
        Version = "2.0.0",
        Description = "Microsserviço gerenciador de vídeos com Clean Architecture e integração MinIO"
    });
});

builder.Services.AddDbContext<VideoManagerDbContext>(options =>
{
    var connectionString = builder.Environment.IsDevelopment()
    ? builder.Configuration.GetConnectionString("DefaultConnection")
    : $"Host={builder.Configuration["DB__Host"]};Port=5432;Database={builder.Configuration["DB__Name"]};Username={builder.Configuration["DB__Username"]};Password={builder.Configuration["DB__Password"]}";
    options.UseNpgsql(connectionString, npgsqlOptions =>
    {
        npgsqlOptions.MigrationsHistoryTable("__EFMigrationsHistory");
        npgsqlOptions.EnableRetryOnFailure(
            maxRetryCount: 3,
            maxRetryDelay: TimeSpan.FromSeconds(5),
            errorCodesToAdd: null);
    });
    
    if (builder.Environment.IsDevelopment())
    {
        options.EnableSensitiveDataLogging();
        options.EnableDetailedErrors();
    }
});

builder.Services.AddSingleton<IAmazonS3>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var minioEndpoint = config["MinIO:Endpoint"];
    
    // Se MinIO:Endpoint configurado, usar MinIO (dev local)
    if (!string.IsNullOrEmpty(minioEndpoint))
    {
        var minioConfig = new AmazonS3Config
        {
            ServiceURL = $"http://{minioEndpoint}",
            ForcePathStyle = true,
            UseHttp = !config.GetValue<bool>("MinIO:UseSSL")
        };
        var credentials = new BasicAWSCredentials(
            config["MinIO:AccessKey"],
            config["MinIO:SecretKey"]
        );
        return new AmazonS3Client(credentials, minioConfig);
    }
    
    // Senão, usar AWS S3 real (produção)
    var s3Config = new AmazonS3Config
    {
        RegionEndpoint = Amazon.RegionEndpoint.GetBySystemName(config["Aws:Region"] ?? "us-east-1"),
        ForcePathStyle = true
    };
    return new AmazonS3Client(s3Config);
});

builder.Services.AddScoped<IVideoRepository, EFVideoRepository>();
builder.Services.AddSingleton<IFileStorageService, MinIOStorageService>();

builder.Services.AddHttpClient<INotificationServiceClient, NotificationServiceClient>();

builder.Services.AddScoped<IUploadVideoUseCase, UploadVideoUseCase>();
builder.Services.AddScoped<IGetUserVideosUseCase, GetUserVideosUseCase>();
builder.Services.AddScoped<IGetVideoStatusUseCase, GetVideoStatusUseCase>();
builder.Services.AddScoped<IUpdateVideoStatusUseCase, UpdateVideoStatusUseCase>();

builder.Services.AddHealthChecks()
    .AddNpgSql(
        connectionString: builder.Configuration.GetConnectionString("DefaultConnection")!,
        name: "postgresql",
        tags: new[] { "db", "postgresql" });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    using (var scope = app.Services.CreateScope())
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<VideoManagerDbContext>();
        try
        {
            await dbContext.Database.MigrateAsync();
            app.Logger.LogInformation("Database migrations applied successfully");
        }
        catch (Exception ex)
        {
            app.Logger.LogWarning(ex, "Could not apply migrations. Database might not be available yet.");
        }
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Video Manager Service API v2.0");
    });
}

app.UseCors();
app.UseRouting();
app.UseHttpMetrics(); // coleta métricas HTTP (latência, status codes, throughput)
app.UseAuthorization();
app.MapControllers();
app.MapMetrics(); // expõe /metrics para o Prometheus raspar

app.MapHealthChecks("/health", new Microsoft.AspNetCore.Diagnostics.HealthChecks.HealthCheckOptions
{
    ResponseWriter = async (context, report) =>
    {
        context.Response.ContentType = "application/json";
        var result = System.Text.Json.JsonSerializer.Serialize(new
        {
            status = report.Status.ToString(),
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds
            }),
            totalDuration = report.TotalDuration.TotalMilliseconds
        });
        await context.Response.WriteAsync(result);
    }
});

app.MapGet("/healthz", () => Results.Ok(new
{
    status = "healthy",
    service = "fiap-x-video-manager-service",
    architecture = "clean-architecture",
    database = "postgresql",
    storage = "minio",
    version = "2.0.0",
    timestamp = DateTime.UtcNow
}));

app.Run();
