using DownloadCenter.Application.Products;
using DownloadCenter.Application.Products.Dtos;
using DownloadCenter.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DownloadCenter.Api.Controllers;

[ApiController]
[Route("api/product-access")]
[Authorize(Roles = "Admin")]
public class ProductAccessController : ControllerBase
{
    private readonly ProductAccessService _productAccessService;

    public ProductAccessController(ProductAccessService productAccessService)
    {
        _productAccessService = productAccessService;
    }

    [HttpPut("{userId}/access-all")]
    public async Task<ApiResult> SetAccessAll(long userId, [FromBody] bool canAccessAll)
    {
        return await _productAccessService.SetAccessAllProductsAsync(userId, canAccessAll);
    }

    [HttpGet("{userId}/products")]
    public async Task<ApiResult> GetAuthorizedProducts(long userId)
    {
        return await _productAccessService.GetAuthorizedProductsAsync(userId);
    }

    [HttpPut("{userId}/products")]
    public async Task<ApiResult> SetAuthorizedProducts(long userId, [FromBody] SetAuthorizedProductsRequest request)
    {
        return await _productAccessService.SetAuthorizedProductsAsync(userId, request);
    }
}
