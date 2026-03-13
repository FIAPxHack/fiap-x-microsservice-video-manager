using VideoManagerService.Domain.Enums;

namespace VideoManagerService.Application.DTOs;

public class UpdateVideoStatusRequestDto
{
    public ProcessingStatus Status { get; set; }
    public string? ZipFileName { get; set; }
    public int FrameCount { get; set; }
    public string? ErrorMessage { get; set; }
}
