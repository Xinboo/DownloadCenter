using DownloadCenter.Domain.Common;

namespace DownloadCenter.Domain.Entities;

public class ProductFile : EntityBase
{
    public long ProductId { get; set; }
    public long FileId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string Description { get; set; } = null!;
}
