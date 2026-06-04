namespace DownloadCenter.Application.Products.Dtos;

public class SetAuthorizedProductsRequest
{
    public List<long> ProductIds { get; set; } = [];
}
