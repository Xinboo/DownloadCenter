namespace DownloadCenter.Shared.Options;

public class FileStorageOptions
{
    public string BasePath { get; set; } = "uploads";
    public long MaxSizeBytes { get; set; } = 104857600;
    public string[] AllowedExtensions { get; set; } = [".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx", ".txt", ".csv", ".zip", ".rar", ".7z", ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".svg"];
}
