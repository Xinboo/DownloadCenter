using DownloadCenter.Domain.Enums;

namespace DownloadCenter.Application.Users.Dtos;

public class UpdateUserRequest
{
    public UserRole Role { get; set; }
    public string NickName { get; set; } = null!;
    public string? Company { get; set; }
    public string? Phone { get; set; }
}
