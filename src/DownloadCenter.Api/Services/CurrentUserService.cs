using System.Security.Claims;
using DownloadCenter.Shared.Auth;

namespace DownloadCenter.Api.Services;

public static class CurrentUserService
{
    public static IServiceCollection AddCurrentUser(this IServiceCollection services)
    {
        services.AddHttpContextAccessor();
        services.AddScoped(sp =>
        {
            var httpContext = sp.GetRequiredService<IHttpContextAccessor>().HttpContext;
            var user = httpContext?.User;

            if (user?.Identity?.IsAuthenticated != true)
                return new CurrentUser();

            return new CurrentUser
            {
                UserId = long.TryParse(user.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : 0,
                UserName = user.FindFirstValue(ClaimTypes.Name) ?? string.Empty,
                NickName = user.FindFirstValue(ClaimTypes.GivenName) ?? string.Empty,
                Role = user.FindFirstValue(ClaimTypes.Role) ?? string.Empty,
                CanAccessAllProducts = bool.TryParse(user.FindFirstValue("CanAccessAllProducts"), out var canAccess) && canAccess,
                IsAuthenticated = true
            };
        });

        return services;
    }
}
