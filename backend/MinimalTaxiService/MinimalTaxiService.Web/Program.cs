using System.Security.Claims;
using System.Text;
using Amazon.S3;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using MinimalTaxiService.Application;
using MinimalTaxiService.Application.Trips.Events;
using MinimalTaxiService.Infrastructure.Postgres;
using MinimalTaxiService.Web.Integrations.Dadata;
using MinimalTaxiService.Web.Integrations.Storage;
using MinimalTaxiService.Web.Integrations.Yandex;
using MinimalTaxiService.Web.Background;
using MinimalTaxiService.Web.Internal;
using MinimalTaxiService.Web.Realtime;
using MinimalTaxiService.Web.Realtime.Kafka;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddOpenApi();

var redisConnectionString = builder.Configuration.GetConnectionString("Redis");
if (!string.IsNullOrWhiteSpace(redisConnectionString))
{
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConnectionString;
    });
}

builder.Services.AddHybridCache();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<YandexOptions>(builder.Configuration.GetSection("Yandex"));
builder.Services.AddHttpClient<YandexGeocodingService>();
builder.Services.Configure<DadataOptions>(builder.Configuration.GetSection("Dadata"));
builder.Services.AddHttpClient<DadataGeocodingService>();
builder.Services.Configure<SelectelS3Options>(builder.Configuration.GetSection("S3"));
builder.Services.Configure<InternalApiOptions>(builder.Configuration.GetSection(InternalApiOptions.SectionName));
builder.Services.AddSingleton<IAmazonS3>(provider =>
{
    var options = provider.GetRequiredService<Microsoft.Extensions.Options.IOptions<SelectelS3Options>>().Value;
    var config = new AmazonS3Config
    {
        ServiceURL = options.Endpoint,
        AuthenticationRegion = options.Region,
        ForcePathStyle = true,
    };

    return new AmazonS3Client(options.AccessKeyId, options.SecretAccessKey, config);
});
builder.Services.AddScoped<SelectelS3StorageService>();
builder.Services.Configure<KafkaEventsOptions>(builder.Configuration.GetSection(KafkaEventsOptions.SectionName));

var kafkaOptions = builder.Configuration.GetSection(KafkaEventsOptions.SectionName).Get<KafkaEventsOptions>();
if (kafkaOptions?.Enabled == true)
{
    builder.Services.AddSingleton<KafkaTripEventsBus>();
    builder.Services.AddSingleton<ITripEventsBus>(provider => provider.GetRequiredService<KafkaTripEventsBus>());
    builder.Services.AddHostedService(provider => provider.GetRequiredService<KafkaTripEventsBus>());
}
else
{
    builder.Services.AddSingleton<ITripEventsBus, InMemoryTripEventsBus>();
}

builder.Services.AddHostedService<TripAutoCancellationService>();

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins("http://localhost:5173")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var jwtSection = builder.Configuration.GetSection("Jwt");
        var issuer = jwtSection["Issuer"];
        var audience = jwtSection["Audience"];
        var secretKey = jwtSection["SecretKey"];
        var accessTokenCookieName = jwtSection["AccessTokenCookieName"] ?? "mt_access_token";

        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = issuer,
            ValidateAudience = true,
            ValidAudience = audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey ?? string.Empty)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.FromMinutes(1),
            NameClaimType = ClaimTypes.NameIdentifier,
            RoleClaimType = ClaimTypes.Role,
        };

        options.Events = new JwtBearerEvents
        {
            OnMessageReceived = context =>
            {
                if (context.Request.Cookies.TryGetValue(accessTokenCookieName, out var cookieToken) && !string.IsNullOrWhiteSpace(cookieToken))
                {
                    context.Token = cookieToken;
                    return Task.CompletedTask;
                }

                var header = context.Request.Headers.Authorization.ToString();
                if (header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                    context.Token = header["Bearer ".Length..].Trim();

                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
}

app.UseCors();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();