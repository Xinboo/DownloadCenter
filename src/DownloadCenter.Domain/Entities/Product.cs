using DownloadCenter.Domain.Common;

namespace DownloadCenter.Domain.Entities;

public class Product : EntityBase
{
    public string Name { get; set; } = null!;
    public string Model { get; set; } = null!;
    public string? Description { get; set; }
    public long? CoverFileId { get; set; }
}
