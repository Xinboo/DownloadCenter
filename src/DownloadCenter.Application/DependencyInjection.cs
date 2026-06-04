using System.Reflection;
using DownloadCenter.Application.Auth;
using DownloadCenter.Application.Files;
using DownloadCenter.Application.Products;
using DownloadCenter.Application.Users;
using Mapster;
using MapsterMapper;
using Microsoft.Extensions.DependencyInjection;

namespace DownloadCenter.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var config = TypeAdapterConfig.GlobalSettings;
        config.Scan(Assembly.GetExecutingAssembly());

        services.AddSingleton(config);
        services.AddScoped<IMapper, ServiceMapper>();

        services.AddScoped<AuthService>();
        services.AddScoped<UserService>();
        services.AddScoped<FileService>();
        services.AddScoped<ProductService>();
        services.AddScoped<ProductAccessService>();

        return services;
    }
}
