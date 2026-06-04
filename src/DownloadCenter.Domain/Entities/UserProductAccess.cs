using DownloadCenter.Domain.Common;

namespace DownloadCenter.Domain.Entities;

public class UserProductAccess : EntityBase
{
    public long UserId { get; set; }
    public bool CanAccessAll { get; set; }
}
