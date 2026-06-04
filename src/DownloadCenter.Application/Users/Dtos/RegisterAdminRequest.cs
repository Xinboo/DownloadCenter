namespace DownloadCenter.Application.Users.Dtos;

public class RegisterAdminRequest
{
    public string UserName { get; set; } = null!;
    public string Password { get; set; } = null!;
    public string NickName { get; set; } = null!;
}
