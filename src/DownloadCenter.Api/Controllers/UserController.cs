using DownloadCenter.Application.Users;
using DownloadCenter.Application.Users.Dtos;
using DownloadCenter.Shared.Auth;
using DownloadCenter.Shared.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DownloadCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly UserService _userService;
    private readonly CurrentUser _currentUser;

    public UserController(UserService userService, CurrentUser currentUser)
    {
        _userService = userService;
        _currentUser = currentUser;
    }

    [HttpGet("status")]
    [AllowAnonymous]
    public async Task<ApiResult> GetStatus()
    {
        return await _userService.GetSystemStatusAsync();
    }

    [HttpPost("register-admin")]
    [AllowAnonymous]
    public async Task<ApiResult> RegisterAdmin([FromBody] RegisterAdminRequest request)
    {
        return await _userService.RegisterAdminAsync(request);
    }

    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResult> GetPage([FromQuery] UserPageInput input)
    {
        return await _userService.GetPageAsync(input);
    }

    [HttpGet("list")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResult> GetList([FromQuery] UserListInput input)
    {
        return await _userService.GetListAsync(input);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResult> Create([FromBody] CreateUserRequest request)
    {
        return await _userService.CreateAsync(request);
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResult> Delete(long id)
    {
        return await _userService.DeleteAsync(id);
    }

    [HttpPut("change-password")]
    [Authorize]
    public async Task<ApiResult> ChangePassword([FromBody] ChangePasswordRequest request)
    {
        return await _userService.ChangePasswordAsync(_currentUser.UserId, request);
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<ApiResult> Update(long id, [FromBody] UpdateUserRequest request)
    {
        return await _userService.UpdateAsync(id, request);
    }
}
