using DownloadCenter.Application.Auth;
using DownloadCenter.Application.Auth.Dtos;
using DownloadCenter.Shared.Models;
using Microsoft.AspNetCore.Mvc;

namespace DownloadCenter.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly AuthService _authService;

    public AuthController(AuthService authService)
    {
        _authService = authService;
    }

    [HttpPost("login")]
    public async Task<ApiResult> Login([FromBody] LoginRequest request)
    {
        return await _authService.LoginAsync(request);
    }

    [HttpPost("refresh")]
    public async Task<ApiResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        return await _authService.RefreshTokenAsync(request);
    }
}
