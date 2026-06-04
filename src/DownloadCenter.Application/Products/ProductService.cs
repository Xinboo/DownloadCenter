using DownloadCenter.Application.Files;
using DownloadCenter.Application.Products.Dtos;
using DownloadCenter.Domain.Entities;
using DownloadCenter.Domain.Enums;
using DownloadCenter.Infrastructure.Persistence;
using DownloadCenter.Shared.Auth;
using DownloadCenter.Shared.Extensions;
using DownloadCenter.Shared.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;

namespace DownloadCenter.Application.Products;

public class ProductService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;
    private readonly FileService _fileService;
    private readonly CurrentUser _currentUser;

    public ProductService(AppDbContext db, IMapper mapper, FileService fileService, CurrentUser currentUser)
    {
        _db = db;
        _mapper = mapper;
        _fileService = fileService;
        _currentUser = currentUser;
    }

    private bool CanAccessAll()
    {
        if (_currentUser.Role == nameof(UserRole.Admin)) return true;
        return _currentUser.CanAccessAllProducts;
    }

    private IQueryable<Product> GetAccessibleProducts()
    {
        var query = _db.Products.AsQueryable();

        if (!CanAccessAll())
        {
            var authorizedProductIds = _db.UserProducts
                .Where(up => up.UserId == _currentUser.UserId)
                .Select(up => up.ProductId);

            query = query.Where(p => authorizedProductIds.Contains(p.Id));
        }

        return query;
    }

    public async Task<ApiResult> GetPageAsync(ProductPageInput input)
    {
        var query = GetAccessibleProducts();

        if (!string.IsNullOrWhiteSpace(input.Name))
            query = query.Where(p => p.Name.Contains(input.Name));
        if (!string.IsNullOrWhiteSpace(input.Model))
            query = query.Where(p => p.Model.Contains(input.Model));

        query = query.OrderByDescending(p => p.CreatedAt);
        var totalCount = await query.CountAsync();

        var products = await query
            .Page(input)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Model = p.Model,
                Description = p.Description,
                CoverFileId = p.CoverFileId,
                FileCount = _db.ProductFiles.Count(pf => pf.ProductId == p.Id),
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return ApiResult.Ok(new PagedResult<ProductDto>
        {
            Items = products,
            TotalCount = totalCount,
            PageNumber = input.PageNumber,
            PageSize = input.PageSize
        });
    }

    public async Task<ApiResult> GetListAsync(ProductListInput input)
    {
        var query = GetAccessibleProducts();

        if (!string.IsNullOrWhiteSpace(input.Name))
            query = query.Where(p => p.Name.Contains(input.Name));
        if (!string.IsNullOrWhiteSpace(input.Model))
            query = query.Where(p => p.Model.Contains(input.Model));

        var products = await query
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Name = p.Name,
                Model = p.Model,
                Description = p.Description,
                CoverFileId = p.CoverFileId,
                FileCount = _db.ProductFiles.Count(pf => pf.ProductId == p.Id),
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();

        return ApiResult.Ok(products);
    }

    public async Task<ApiResult> GetByIdAsync(long id)
    {
        var product = await GetAccessibleProducts().FirstOrDefaultAsync(p => p.Id == id);
        if (product == null)
            return ApiResult.Fail("产品不存在或无权访问");

        var dto = new ProductDetailDto
        {
            Id = product.Id,
            Name = product.Name,
            Model = product.Model,
            Description = product.Description,
            CoverFileId = product.CoverFileId,
            CreatedAt = product.CreatedAt
        };

        return ApiResult.Ok(dto);
    }

    public async Task<ApiResult> GetResourcePageAsync(long productId, ProductResourcePageInput input)
    {
        var product = await GetAccessibleProducts().FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null)
            return ApiResult.Fail("产品不存在或无权访问");

        var query = _db.ProductFiles
            .Where(pf => pf.ProductId == productId)
            .Join(_db.FileRecords,
                pf => pf.FileId,
                f => f.Id,
                (pf, f) => new ProductFileDto
                {
                    FileId = f.Id,
                    DisplayName = pf.DisplayName,
                    Description = pf.Description,
                    Extension = f.Extension,
                    Size = f.Size,
                    CreatedAt = f.CreatedAt
                });

        if (!string.IsNullOrWhiteSpace(input.DisplayName))
            query = query.Where(pf => pf.DisplayName.Contains(input.DisplayName));

        var orderedQuery = query.OrderByDescending(pf => pf.CreatedAt);
        var totalCount = await orderedQuery.CountAsync();
        var items = await orderedQuery.Page(input).ToListAsync();

        return ApiResult.Ok(new PagedResult<ProductFileDto>
        {
            Items = items,
            TotalCount = totalCount,
            PageNumber = input.PageNumber,
            PageSize = input.PageSize
        });
    }

    public async Task<ApiResult> CreateAsync(CreateProductRequest request)
    {
        var product = _mapper.Map<Product>(request);

        _db.Products.Add(product);
        await _db.SaveChangesAsync();

        return ApiResult.Ok("创建成功");
    }

    public async Task<ApiResult> UpdateAsync(long id, UpdateProductRequest request)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
            return ApiResult.Fail("产品不存在");

        product.Name = request.Name;
        product.Model = request.Model;
        product.Description = request.Description;
        product.CoverFileId = request.CoverFileId;

        await _db.SaveChangesAsync();

        return ApiResult.Ok("更新成功");
    }

    public async Task<ApiResult> DeleteAsync(long id)
    {
        var product = await _db.Products.FindAsync(id);
        if (product == null)
            return ApiResult.Fail("产品不存在");

        var productFiles = await _db.ProductFiles
            .Where(pf => pf.ProductId == id)
            .ToListAsync();

        foreach (var pf in productFiles)
        {
            await _fileService.DeleteAsync(pf.FileId);
        }

        if (product.CoverFileId.HasValue)
        {
            await _fileService.DeleteAsync(product.CoverFileId.Value);
        }

        _db.ProductFiles.RemoveRange(productFiles);
        _db.Products.Remove(product);
        await _db.SaveChangesAsync();

        return ApiResult.Ok("删除成功");
    }

    public async Task<ApiResult> PublishResourceAsync(long productId, long fileId, PublishResourceRequest request)
    {
        var product = await _db.Products.FindAsync(productId);
        if (product == null)
            return ApiResult.Fail("产品不存在");

        var file = await _db.FileRecords.FindAsync(fileId);
        if (file == null)
            return ApiResult.Fail("文件不存在");

        var exists = await _db.ProductFiles.AnyAsync(pf => pf.ProductId == productId && pf.FileId == fileId);
        if (exists)
            return ApiResult.Fail("该资源已发布");

        var productFile = new ProductFile
        {
            ProductId = productId,
            FileId = fileId,
            DisplayName = request.DisplayName,
            Description = request.Description
        };

        _db.ProductFiles.Add(productFile);
        await _db.SaveChangesAsync();

        return ApiResult.Ok("发布成功");
    }

    public async Task<ApiResult> DeleteResourceAsync(long productId, long fileId)
    {
        var productFile = await _db.ProductFiles
            .FirstOrDefaultAsync(pf => pf.ProductId == productId && pf.FileId == fileId);

        if (productFile == null)
            return ApiResult.Fail("产品资源不存在");

        _db.ProductFiles.Remove(productFile);
        await _db.SaveChangesAsync();

        await _fileService.DeleteAsync(fileId);

        return ApiResult.Ok("删除成功");
    }

    public async Task<(string FullPath, string OriginalName, string Extension)?> GetFileDownloadAsync(long productId, long fileId)
    {
        var product = await GetAccessibleProducts().FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null)
            return null;

        var exists = await _db.ProductFiles.AnyAsync(pf => pf.ProductId == productId && pf.FileId == fileId);
        if (!exists)
            return null;

        return await _fileService.GetDownloadAsync(fileId);
    }

    public async Task<(string FullPath, string OriginalName, string Extension)?> GetCoverPreviewAsync(long productId)
    {
        var product = await GetAccessibleProducts().FirstOrDefaultAsync(p => p.Id == productId);
        if (product?.CoverFileId == null)
            return null;

        return await _fileService.GetDownloadAsync(product.CoverFileId.Value);
    }
}
