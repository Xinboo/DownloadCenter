using DownloadCenter.Domain.Common;

namespace DownloadCenter.Domain.Entities;

public class FileRecord : EntityBase
{
    public string OriginalName { get; set; } = null!;
    public string StoredName { get; set; } = null!;
    public string Extension { get; set; } = null!;
    public string RelativePath { get; set; } = null!;
    public long Size { get; set; }
}
