using DownloadCenter.Application.Users.Dtos;
using DownloadCenter.Domain.Entities;
using DownloadCenter.Domain.Enums;
using DownloadCenter.Infrastructure.Persistence;
using DownloadCenter.Shared.Extensions;
using DownloadCenter.Shared.Models;
using MapsterMapper;
using Microsoft.EntityFrameworkCore;
using Yitter.IdGenerator;

namespace DownloadCenter.Application.Users;

public class UserService
{
    private readonly AppDbContext _db;
    private readonly IMapper _mapper;

    public UserService(AppDbContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    public async Task<ApiResult> GetSystemStatusAsync()
    {
        var initialized = await _db.Users.AnyAsync();
        return ApiResult.Ok(new { Initialized = initialized });
    }

    public async Task<ApiResult> RegisterAdminAsync(RegisterAdminRequest request)
    {
        if (await _db.Users.AnyAsync())
            return ApiResult.Fail("系统已初始化，无法重复注册管理员");

        var user = new User
        {
            Id = YitIdHelper.NextId(),
            UserName = request.UserName,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = UserRole.Admin,
            NickName = request.NickName
        };
        _db.Users.Add(user);

        var access = new UserProductAccess
        {
            UserId = user.Id,
            CanAccessAll = true
        };
        _db.UserProductAccesses.Add(access);

        await _db.SaveChangesAsync();

        return ApiResult.Ok("管理员注册成功");
    }

    public async Task<ApiResult> GetPageAsync(UserPageInput input)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(input.UserName))
            query = query.Where(u => u.UserName.Contains(input.UserName));
        if (!string.IsNullOrWhiteSpace(input.NickName))
            query = query.Where(u => u.NickName.Contains(input.NickName));

        query = query.OrderByDescending(u => u.CreatedAt);
        var totalCount = await query.CountAsync();
        var users = await query.Page(input).ToListAsync();
        var dtos = _mapper.Map<List<UserDto>>(users);
        return ApiResult.Ok(new PagedResult<UserDto>
        {
            Items = dtos,
            TotalCount = totalCount,
            PageNumber = input.PageNumber,
            PageSize = input.PageSize
        });
    }

    public async Task<ApiResult> GetListAsync(UserListInput input)
    {
        var query = _db.Users.AsQueryable();

        if (!string.IsNullOrWhiteSpace(input.UserName))
            query = query.Where(u => u.UserName.Contains(input.UserName));
        if (!string.IsNullOrWhiteSpace(input.NickName))
            query = query.Where(u => u.NickName.Contains(input.NickName));

        var users = await query.OrderByDescending(u => u.CreatedAt).ToListAsync();
        var dtos = _mapper.Map<List<UserDto>>(users);
        return ApiResult.Ok(dtos);
    }

    public async Task<ApiResult> CreateAsync(CreateUserRequest request)
    {
        if (await _db.Users.AnyAsync(u => u.UserName == request.UserName))
            return ApiResult.Fail("用户名已存在");

        var user = _mapper.Map<User>(request);
        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password);

        user.Id = YitIdHelper.NextId();
        _db.Users.Add(user);

        var access = new UserProductAccess
        {
            UserId = user.Id,
            CanAccessAll = request.Role == UserRole.Admin
        };
        _db.UserProductAccesses.Add(access);

        await _db.SaveChangesAsync();

        return ApiResult.Ok("创建成功");
    }

    public async Task<ApiResult> DeleteAsync(long id)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return ApiResult.Fail("用户不存在");

        _db.Users.Remove(user);
        await _db.SaveChangesAsync();

        return ApiResult.Ok("删除成功");
    }

    public async Task<ApiResult> ChangePasswordAsync(long userId, ChangePasswordRequest request)
    {
        if (request.NewPassword != request.ConfirmPassword)
            return ApiResult.Fail("两次输入的密码不一致");

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return ApiResult.Fail("用户不存在");

        if (!BCrypt.Net.BCrypt.Verify(request.OldPassword, user.PasswordHash))
            return ApiResult.Fail("原密码错误");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        await _db.SaveChangesAsync();

        return ApiResult.Ok("密码修改成功");
    }

    public async Task<ApiResult> UpdateAsync(long id, UpdateUserRequest request)
    {
        var user = await _db.Users.FindAsync(id);
        if (user == null)
            return ApiResult.Fail("用户不存在");

        user.Role = request.Role;
        user.NickName = request.NickName;
        user.Company = request.Company;
        user.Phone = request.Phone;
        await _db.SaveChangesAsync();

        return ApiResult.Ok("更新成功");
    }
}
