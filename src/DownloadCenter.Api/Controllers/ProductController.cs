using DownloadCenter.Application.Products;
using DownloadCenter.Application.Products.Dtos;
using DownloadCenter.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;

namespace DownloadCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ProductController : ControllerBase
{
    private readonly ProductService _productService;

    public ProductController(ProductService productService)
    {
        _productService = productService;
    }

    [HttpGet]
    public async Task<ApiResult> GetPage([FromQuery] ProductPageInput input)
    {
        return await _productService.GetPageAsync(input);
    }

    [HttpGet("list")]
    public async Task<ApiResult> GetList([FromQuery] ProductListInput input)
    {
        return await _productService.GetListAsync(input);
    }

    [HttpGet("{id}")]
    public async Task<ApiResult> GetById(long id)
    {
        return await _productService.GetByIdAsync(id);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResult> Create([FromBody] CreateProductRequest request)
    {
        return await _productService.CreateAsync(request);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResult> Update(long id, [FromBody] UpdateProductRequest request)
    {
        return await _productService.UpdateAsync(id, request);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResult> Delete(long id)
    {
        return await _productService.DeleteAsync(id);
    }

    [HttpGet("{id}/resource")]
    public async Task<ApiResult> GetResourcePage(long id, [FromQuery] ProductResourcePageInput input)
    {
        return await _productService.GetResourcePageAsync(id, input);
    }

    [HttpPost("{id}/resource/{fileId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResult> PublishResource(long id, long fileId, [FromBody] PublishResourceRequest request)
    {
        return await _productService.PublishResourceAsync(id, fileId, request);
    }

    [HttpDelete("{id}/resource/{fileId}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResult> DeleteResource(long id, long fileId)
    {
        return await _productService.DeleteResourceAsync(id, fileId);
    }

    [HttpGet("{id}/resource/{fileId}/download")]
    public async Task<IActionResult> DownloadFile(long id, long fileId)
    {
        var result = await _productService.GetFileDownloadAsync(id, fileId);
        if (result == null)
            return NotFound(ApiResult.Fail("文件不存在或无权访问"));

        var (fullPath, originalName, _) = result.Value;

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(originalName, out var contentType))
            contentType = "application/octet-stream";

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return File(stream, contentType, originalName);
    }

    [HttpGet("{id}/cover")]
    public async Task<IActionResult> CoverPreview(long id)
    {
        var result = await _productService.GetCoverPreviewAsync(id);
        if (result == null)
            return NotFound(ApiResult.Fail("封面图不存在或无权访问"));

        var (fullPath, originalName, _) = result.Value;

        var provider = new FileExtensionContentTypeProvider();
        if (!provider.TryGetContentType(originalName, out var contentType))
            contentType = "application/octet-stream";

        var stream = new FileStream(fullPath, FileMode.Open, FileAccess.Read);
        return File(stream, contentType);
    }
}
