using DownloadCenter.Shared.Models;

namespace DownloadCenter.Application.Users.Dtos;

public class UserPageInput : PagedRequest
{
    public string? UserName { get; set; }
    public string? NickName { get; set; }
}
