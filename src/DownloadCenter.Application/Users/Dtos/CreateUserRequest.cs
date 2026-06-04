using DownloadCenter.Domain.Enums;

namespace DownloadCenter.Application.Users.Dtos;

public class CreateUserRequest
{
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public UserRole Role { get; set; }
    public string NickName { get; set; } = null!;
    public string? Company { get; set; }
    public string? Phone { get; set; }
}
