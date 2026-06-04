namespace DownloadCenter.Application.Products.Dtos;

public class ProductDetailDto
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string Model { get; set; } = null!;
    public string? Description { get; set; }
    public long? CoverFileId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class ProductFileDto
{
    public long FileId { get; set; }
    public string DisplayName { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string Extension { get; set; } = null!;
    public long Size { get; set; }
    public DateTime CreatedAt { get; set; }
}
