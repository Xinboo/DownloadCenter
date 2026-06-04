namespace DownloadCenter.Application.Files.Dtos;

public class FileDto
{
    public long Id { get; set; }
    public string OriginalName { get; set; } = null!;
    public string Extension { get; set; } = null!;
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
}
