namespace DownloadCenter.Application.Products.Dtos;

public class ProductDto
{
    public long Id { get; set; }
    public string Name { get; set; } = null!;
    public string Model { get; set; } = null!;
    public string? Description { get; set; }
    public long? CoverFileId { get; set; }
    public int FileCount { get; set; }
    public DateTime CreatedAt { get; set; }
}
