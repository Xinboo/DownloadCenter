using DownloadCenter.Application.Products.Dtos;
using DownloadCenter.Domain.Entities;
using DownloadCenter.Infrastructure.Persistence;
using DownloadCenter.Shared.Models;
using Microsoft.EntityFrameworkCore;

namespace DownloadCenter.Application.Products;

public class ProductAccessService
{
    private readonly AppDbContext _db;

    public ProductAccessService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<ApiResult> SetAccessAllProductsAsync(long userId, bool canAccessAll)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return ApiResult.Fail("用户不存在");

        var access = await _db.UserProductAccesses.FirstOrDefaultAsync(x => x.UserId == userId);
        if (access == null)
        {
            access = new UserProductAccess { UserId = userId, CanAccessAll = canAccessAll };
            _db.UserProductAccesses.Add(access);
        }
        else
        {
            access.CanAccessAll = canAccessAll;
        }

        await _db.SaveChangesAsync();

        return ApiResult.Ok(canAccessAll ? "已开启查看全部产品" : "已关闭查看全部产品");
    }

    public async Task<ApiResult> SetAuthorizedProductsAsync(long userId, SetAuthorizedProductsRequest request)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return ApiResult.Fail("用户不存在");

        var existing = await _db.UserProducts.Where(up => up.UserId == userId).ToListAsync();
        _db.UserProducts.RemoveRange(existing);

        foreach (var productId in request.ProductIds)
        {
            _db.UserProducts.Add(new UserProduct
            {
                UserId = userId,
                ProductId = productId
            });
        }

        await _db.SaveChangesAsync();

        return ApiResult.Ok("授权设置成功");
    }

    public async Task<ApiResult> GetAuthorizedProductsAsync(long userId)
    {
        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return ApiResult.Fail("用户不存在");

        var access = await _db.UserProductAccesses.FirstOrDefaultAsync(x => x.UserId == userId);
        var canAccessAll = access?.CanAccessAll ?? false;

        var products = await _db.UserProducts
            .Where(up => up.UserId == userId)
            .Join(_db.Products,
                up => up.ProductId,
                p => p.Id,
                (up, p) => new { p.Id, p.Name, p.Model })
            .ToListAsync();

        return ApiResult.Ok(new
        {
            CanAccessAllProducts = canAccessAll,
            Products = products
        });
    }
}
