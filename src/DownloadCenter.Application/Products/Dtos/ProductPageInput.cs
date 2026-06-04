using DownloadCenter.Shared.Models;

namespace DownloadCenter.Application.Products.Dtos;

public class ProductPageInput : PagedRequest
{
    public string? Name { get; set; }
    public string? Model { get; set; }
}
