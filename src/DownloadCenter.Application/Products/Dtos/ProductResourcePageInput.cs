using DownloadCenter.Shared.Models;

namespace DownloadCenter.Application.Products.Dtos;

public class ProductResourcePageInput : PagedRequest
{
    public string? DisplayName { get; set; }
}
