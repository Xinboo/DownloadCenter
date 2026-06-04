namespace DownloadCenter.Application.Products.Dtos;

public class UpdateProductRequest
{
    public string Name { get; set; } = null!;
    public string Model { get; set; } = null!;
    public string? Description { get; set; }
    public long? CoverFileId { get; set; }
}
