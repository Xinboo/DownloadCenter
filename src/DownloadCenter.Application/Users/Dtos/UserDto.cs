using DownloadCenter.Domain.Enums;

namespace DownloadCenter.Application.Users.Dtos;

public class UserDto
{
    public long Id { get; set; }
    public string UserName { get; set; } = null!;
    public UserRole Role { get; set; }
    public string NickName { get; set; } = null!;
    public string? Company { get; set; }
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; }
}
