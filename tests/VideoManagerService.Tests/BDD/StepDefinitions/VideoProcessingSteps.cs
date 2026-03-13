using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using VideoManagerService.Application.DTOs;
using VideoManagerService.Application.Interfaces;
using VideoManagerService.Application.UseCases;
using VideoManagerService.Domain.Entities;
using VideoManagerService.Domain.Enums;
using VideoManagerService.Domain.Interfaces.Repositories;
using VideoManagerService.Domain.Interfaces.Services;
using TechTalk.SpecFlow;

namespace VideoManagerService.Tests.BDD.StepDefinitions;

[Binding]
public class VideoProcessingSteps
{
    private readonly Mock<IVideoRepository> _mockRepository;
    private readonly Mock<IFileStorageService> _mockStorageService;
    private readonly Mock<ILogger<UploadVideoUseCase>> _mockUploadLogger;
    private readonly Mock<ILogger<GetUserVideosUseCase>> _mockGetVideosLogger;
    private readonly Mock<ILogger<GetVideoStatusUseCase>> _mockGetStatusLogger;

    private UploadVideoUseCase _uploadUseCase;
    private GetUserVideosUseCase _getUserVideosUseCase;
    private GetVideoStatusUseCase _getVideoStatusUseCase;

    private string? _userId;
    private string? _email;
    private string? _fileName;
    private long _fileSize;
    private VideoUploadResponseDto? _uploadResult;
    private IEnumerable<VideoInfoDto>? _videosResult;
    private VideoInfoDto? _statusResult;
    private VideoUpload? _currentVideo;

    public VideoProcessingSteps()
    {
        _mockRepository = new Mock<IVideoRepository>();
        _mockStorageService = new Mock<IFileStorageService>();
        _mockUploadLogger = new Mock<ILogger<UploadVideoUseCase>>();
        _mockGetVideosLogger = new Mock<ILogger<GetUserVideosUseCase>>();
        _mockGetStatusLogger = new Mock<ILogger<GetVideoStatusUseCase>>();

        SetupMocks();
        CreateUseCases();
    }

    private void SetupMocks()
    {
        _mockStorageService.Setup(x => x.SaveFileAsync(It.IsAny<Stream>(), It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("/storage/path");

        _mockRepository.Setup(x => x.GetByUserIdAsync(It.IsAny<string>()))
            .ReturnsAsync(new List<VideoUpload>());

        _mockRepository.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
            .ReturnsAsync((string id) => _currentVideo?.Id == id ? _currentVideo : null);
    }

    private void CreateUseCases()
    {
        _uploadUseCase = new UploadVideoUseCase(
            _mockRepository.Object,
            _mockStorageService.Object,
            _mockUploadLogger.Object);

        _getUserVideosUseCase = new GetUserVideosUseCase(
            _mockRepository.Object,
            _mockGetVideosLogger.Object);

        _getVideoStatusUseCase = new GetVideoStatusUseCase(
            _mockRepository.Object,
            _mockGetStatusLogger.Object);
    }

    [Given(@"que sou um usuário com ID ""(.*)"" e email ""(.*)""")]
    public void DadoQueSouUmUsuarioComIDEEmail(string userId, string email)
    {
        _userId = userId;
        _email = email;
    }

    [Given(@"que fiz upload de (.*) vídeos anteriormente")]
    public void DadoQueFizUploadDeVideosAnteriormente(int quantidade)
    {
        var videos = new List<VideoUpload>();
        for (int i = 0; i < quantidade; i++)
        {
            videos.Add(new VideoUpload(_userId!, $"video{i}.mp4", $"stored{i}.mp4", $"/path{i}", 1000));
        }

        _mockRepository.Setup(x => x.GetByUserIdAsync(_userId!))
            .ReturnsAsync(videos);
    }

    [Given(@"que um vídeo com ID ""(.*)"" está em processamento")]
    public void DadoQueUmVideoComIDEstaEmProcessamento(string videoId)
    {
        _currentVideo = new VideoUpload("user", "video.mp4", "stored_video.mp4", "/path", 1000);
        _currentVideo.StartProcessing();
        
        _mockRepository.Setup(x => x.GetByIdAsync(videoId))
            .ReturnsAsync(_currentVideo);
    }

    [Given(@"que tenho um vídeo pendente com ID ""(.*)""")]
    public void DadoQueTenhoUmVideoPendenteComID(string videoId)
    {
        _currentVideo = new VideoUpload("user", "video.mp4", "stored_video.mp4", "/path", 1000);
        
        _mockRepository.Setup(x => x.GetByIdAsync(videoId))
            .ReturnsAsync(_currentVideo);
    }

    [When(@"eu faço upload de um vídeo ""(.*)"" de (.*) MB")]
    public async Task QuandoEuFacoUploadDeUmVideoDeSize(string fileName, int sizeMB)
    {
        _fileName = fileName;
        _fileSize = sizeMB * 1024 * 1024;
        
        var mockFile = CreateMockFile(fileName, _fileSize);
        var request = new VideoUploadRequestDto
        {
            UserId = _userId!,
            Email = _email!,
            Video = mockFile.Object
        };

        _uploadResult = await _uploadUseCase.ExecuteAsync(request);
    }

    [When(@"eu faço upload de um vídeo ""(.*)""")]
    public async Task QuandoEuFacoUploadDeUmVideo(string fileName)
    {
        _fileName = fileName;
        _fileSize = 10 * 1024 * 1024; // Default 10 MB
        
        var mockFile = CreateMockFile(fileName, _fileSize);
        var request = new VideoUploadRequestDto
        {
            UserId = _userId!,
            Email = _email!,
            Video = mockFile.Object
        };

        _uploadResult = await _uploadUseCase.ExecuteAsync(request);
    }

    [When(@"eu tento fazer upload de um arquivo ""(.*)""")]
    public async Task QuandoEuTentoFazerUploadDeUmArquivo(string fileName)
    {
        _fileName = fileName;
        var mockFile = CreateMockFile(fileName, 1024);
        var request = new VideoUploadRequestDto
        {
            UserId = _userId!,
            Email = _email!,
            Video = mockFile.Object
        };

        _uploadResult = await _uploadUseCase.ExecuteAsync(request);
    }

    [When(@"eu tento fazer upload de um vídeo de (.*) MB")]
    public async Task QuandoEuTentoFazerUploadDeUmVideo(int sizeMB)
    {
        _fileSize = sizeMB * 1024 * 1024;
        var mockFile = CreateMockFile("large-video.mp4", _fileSize);
        var request = new VideoUploadRequestDto
        {
            UserId = _userId!,
            Email = _email!,
            Video = mockFile.Object
        };

        _uploadResult = await _uploadUseCase.ExecuteAsync(request);
    }

    [When(@"eu consulto meus vídeos")]
    public async Task QuandoEuConsultoMeusVideos()
    {
        _videosResult = await _getUserVideosUseCase.ExecuteAsync(_userId!);
    }

    [When(@"eu consulto o status do vídeo")]
    public async Task QuandoEuConsultoOStatusDoVideo()
    {
        _statusResult = await _getVideoStatusUseCase.ExecuteAsync(_currentVideo!.Id);
    }

    [When(@"eu consulto o status de um vídeo inexistente ""(.*)""")]
    public async Task QuandoEuConsultoOStatusDeUmVideoInexistente(string videoId)
    {
        _statusResult = await _getVideoStatusUseCase.ExecuteAsync(videoId);
    }



    [Then(@"o upload deve ser realizado com sucesso")]
    public void EntaoOUploadDeveSerRealizadoComSucesso()
    {
        _uploadResult.Should().NotBeNull();
        _uploadResult!.Success.Should().BeTrue();
    }

    [Then(@"o upload deve falhar")]
    public void EntaoOUploadDeveFalhar()
    {
        _uploadResult.Should().NotBeNull();
        _uploadResult!.Success.Should().BeFalse();
    }

    [Then(@"o status deve ser ""(.*)""")]
    public void EntaoOStatusDeveSer(string status)
    {
        var expectedStatus = Enum.Parse<ProcessingStatus>(status);
        
        if (_statusResult != null)
            _statusResult.Status.Should().Be(expectedStatus);
        else
            _currentVideo!.Status.Should().Be(expectedStatus);
    }

    [Then(@"devo receber um ID de vídeo")]
    public void EntaoDevoReceberUmIDDeVideo()
    {
        _uploadResult!.VideoId.Should().NotBeNullOrEmpty();
    }

    [Then(@"deve mostrar erro de ""(.*)""")]
    public void EntaoDeveMostrarErroDe(string errorMessage)
    {
        _uploadResult!.Message.Should().Contain(errorMessage);
    }

    [Then(@"devo ver (.*) vídeos na lista")]
    public void EntaoDevoVerVideosNaLista(int quantidade)
    {
        _videosResult.Should().HaveCount(quantidade);
    }

    [Then(@"todos devem pertencer ao usuário ""(.*)""")]
    public void EntaoTodosDevemPertencerAoUsuario(string userId)
    {
        _videosResult.Should().AllSatisfy(v => v.UserId.Should().Be(userId));
    }



    [Then(@"o formato deve ser aceito")]
    public void EntaoOFormatoDeveSerAceito()
    {
        _uploadResult!.Success.Should().BeTrue();
    }

    [Then(@"não deve encontrar o vídeo")]
    public void EntaoNaoDeveEncontrarOVideo()
    {
        _statusResult.Should().BeNull();
    }

    private Mock<IFormFile> CreateMockFile(string fileName, long fileSize)
    {
        var mockFile = new Mock<IFormFile>();
        mockFile.Setup(f => f.FileName).Returns(fileName);
        mockFile.Setup(f => f.Length).Returns(fileSize);
        mockFile.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        mockFile.Setup(f => f.ContentType).Returns("video/mp4");
        return mockFile;
    }
}
