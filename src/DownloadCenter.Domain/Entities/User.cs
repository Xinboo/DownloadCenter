using DownloadCenter.Domain.Common;
using DownloadCenter.Domain.Enums;

namespace DownloadCenter.Domain.Entities;

public class User : EntityBase
{
    public string UserName { get; set; } = null!;
    public string PasswordHash { get; set; } = null!;
    public UserRole Role { get; set; }
    public string NickName { get; set; } = null!;
    public string? Company { get; set; }
    public string? Phone { get; set; }
}
