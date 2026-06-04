namespace DownloadCenter.Shared.Auth;

public class CurrentUser
{
    public long UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string NickName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public bool CanAccessAllProducts { get; set; }
    public bool IsAuthenticated { get; set; }
}
