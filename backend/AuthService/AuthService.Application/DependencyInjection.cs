using AuthService.Application.Abstractions;
using AuthService.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, Services.AuthService>();
        return services;
    }
}
