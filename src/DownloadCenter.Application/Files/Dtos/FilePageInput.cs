using DownloadCenter.Shared.Models;

namespace DownloadCenter.Application.Files.Dtos;

public class FilePageInput : PagedRequest
{
    public string? OriginalName { get; set; }
}
