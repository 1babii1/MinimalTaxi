using AuthService.Application.Abstractions;
using AuthService.Infrastructure.PostgreSql.Data;
using AuthService.Infrastructure.PostgreSql.Email;
using AuthService.Infrastructure.PostgreSql.Identity;
using AuthService.Infrastructure.PostgreSql.Integrations.ProfileSync;
using AuthService.Infrastructure.PostgreSql.Urls;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AuthService.Infrastructure.PostgreSql;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<AuthDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("AuthDb")));

        var emailSection = configuration.GetSection(EmailOptions.SectionName);
        var frontendSection = configuration.GetSection(FrontendOptions.SectionName);
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var profileSyncSection = configuration.GetSection(ProfileSyncOptions.SectionName);

        services.Configure<EmailOptions>(options =>
        {
            options.Host = emailSection["Host"] ?? options.Host;
            options.Port = int.TryParse(emailSection["Port"], out var port) ? port : options.Port;
            options.EnableSsl = bool.TryParse(emailSection["EnableSsl"], out var enableSsl) && enableSsl;
            options.From = emailSection["From"] ?? options.From;
            options.FromName = emailSection["FromName"] ?? options.FromName;
            options.User = emailSection["User"] ?? options.User;
            options.Pass = emailSection["Pass"] ?? options.Pass;
        });

        services.Configure<FrontendOptions>(options =>
        {
            options.BaseUrl = frontendSection["BaseUrl"] ?? options.BaseUrl;
        });

        services.Configure<ProfileSyncOptions>(options =>
        {
            options.BaseUrl = profileSyncSection["BaseUrl"] ?? options.BaseUrl;
            options.InternalKey = profileSyncSection["InternalKey"] ?? options.InternalKey;
        });

        services.Configure<JwtOptions>(options =>
        {
            options.Issuer = jwtSection["Issuer"] ?? options.Issuer;
            options.Audience = jwtSection["Audience"] ?? options.Audience;
            options.SecretKey = jwtSection["SecretKey"] ?? options.SecretKey;
            options.AccessTokenExpirationMinutes = int.TryParse(jwtSection["AccessTokenExpirationMinutes"], out var accessExpirationMinutes)
                ? accessExpirationMinutes
                : options.AccessTokenExpirationMinutes;
            options.RefreshTokenExpirationDays = int.TryParse(jwtSection["RefreshTokenExpirationDays"], out var refreshExpirationDays)
                ? refreshExpirationDays
                : options.RefreshTokenExpirationDays;
            options.AccessTokenCookieName = jwtSection["AccessTokenCookieName"] ?? options.AccessTokenCookieName;
            options.RefreshTokenCookieName = jwtSection["RefreshTokenCookieName"] ?? options.RefreshTokenCookieName;
        });

        services.PostConfigure<EmailOptions>(options =>
        {
            options.User = Environment.GetEnvironmentVariable("EMAIL_USER") ?? options.User;
            options.Pass = Environment.GetEnvironmentVariable("EMAIL_PASS") ?? options.Pass;
            options.From = Environment.GetEnvironmentVariable("EMAIL_FROM") ?? options.From;
            options.FromName = Environment.GetEnvironmentVariable("EMAIL_FROM_NAME") ?? options.FromName;
        });

        services.AddScoped<IIdentityAuthGateway, IdentityAuthGateway>();
        services.AddScoped<IJwtTokenProvider, JwtTokenProvider>();
        services.AddScoped<IEmailSender, SmtpEmailSender>();
        services.AddScoped<IFrontendUrlProvider, FrontendUrlProvider>();
        services.AddHttpClient<IProfileSyncGateway, TaxiProfileSyncGateway>((serviceProvider, client) =>
        {
            var options = serviceProvider.GetRequiredService<Microsoft.Extensions.Options.IOptions<ProfileSyncOptions>>().Value;
            client.BaseAddress = new Uri(options.BaseUrl.EndsWith('/') ? options.BaseUrl : $"{options.BaseUrl}/");
        });

        return services;
    }
}
