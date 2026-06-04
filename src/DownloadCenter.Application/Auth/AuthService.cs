using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using DownloadCenter.Application.Auth.Dtos;
using DownloadCenter.Infrastructure.Persistence;
using DownloadCenter.Shared.Models;
using DownloadCenter.Shared.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DownloadCenter.Application.Auth;

public class AuthService
{
    private readonly AppDbContext _db;
    private readonly JwtOptions _jwtOptions;

    public AuthService(AppDbContext db, IOptions<JwtOptions> jwtOptions)
    {
        _db = db;
        _jwtOptions = jwtOptions.Value;
    }

    public async Task<ApiResult> LoginAsync(LoginRequest request)
    {
        var user = await _db.Users.FirstOrDefaultAsync(u => u.UserName == request.UserName);
        if (user == null)
            return ApiResult.Fail("用户名或密码错误");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            return ApiResult.Fail("用户名或密码错误");

        var canAccessAll = await _db.UserProductAccesses
            .AnyAsync(x => x.UserId == user.Id && x.CanAccessAll);

        var expiresAt = DateTime.Now.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);

        var accessToken = GenerateAccessToken(user.Id, user.UserName, user.NickName, user.Role.ToString(), canAccessAll, expiresAt);
        var refreshToken = GenerateRefreshToken(user.Id);

        var response = new LoginResponse
        {
            AccessToken = accessToken,
            RefreshToken = refreshToken,
            ExpiresAt = expiresAt
        };

        return ApiResult.Ok(response, "登录成功");
    }

    public async Task<ApiResult> RefreshTokenAsync(RefreshTokenRequest request)
    {
        var principal = GetPrincipalFromExpiredToken(request.AccessToken);
        if (principal == null)
            return ApiResult.Fail("无效的 AccessToken");

        var refreshPrincipal = ValidateRefreshToken(request.RefreshToken);
        if (refreshPrincipal == null)
            return ApiResult.Fail("RefreshToken 无效或已过期");

        var userIdFromAccess = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        var userIdFromRefresh = refreshPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);

        if (userIdFromAccess != userIdFromRefresh)
            return ApiResult.Fail("Token 不匹配");

        if (!long.TryParse(userIdFromAccess, out var userId))
            return ApiResult.Fail("无效的用户信息");

        var user = await _db.Users.FindAsync(userId);
        if (user == null)
            return ApiResult.Fail("用户不存在");

        var canAccessAll = await _db.UserProductAccesses
            .AnyAsync(x => x.UserId == user.Id && x.CanAccessAll);

        var expiresAt = DateTime.Now.AddMinutes(_jwtOptions.AccessTokenExpirationMinutes);

        var newAccessToken = GenerateAccessToken(user.Id, user.UserName, user.NickName, user.Role.ToString(), canAccessAll, expiresAt);
        var newRefreshToken = GenerateRefreshToken(user.Id);

        var response = new LoginResponse
        {
            AccessToken = newAccessToken,
            RefreshToken = newRefreshToken,
            ExpiresAt = expiresAt
        };

        return ApiResult.Ok(response);
    }

    private string GenerateAccessToken(long userId, string userName, string nickName, string role, bool canAccessAllProducts, DateTime expiresAt)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
            new Claim(ClaimTypes.Name, userName),
            new Claim(ClaimTypes.GivenName, nickName),
            new Claim(ClaimTypes.Role, role),
            new Claim("CanAccessAllProducts", canAccessAllProducts.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private string GenerateRefreshToken(long userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: _jwtOptions.Issuer,
            audience: _jwtOptions.Audience,
            claims: claims,
            expires: DateTime.Now.AddDays(_jwtOptions.RefreshTokenExpirationDays),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey)),
            ValidateLifetime = false
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, tokenValidationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }

    private ClaimsPrincipal? ValidateRefreshToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtOptions.SecretKey)),
            ValidateLifetime = true
        };

        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, tokenValidationParameters, out _);
            return principal;
        }
        catch
        {
            return null;
        }
    }
}
