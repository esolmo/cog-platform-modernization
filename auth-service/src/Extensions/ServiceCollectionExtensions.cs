using AuthService.Configuration;
using AuthService.Data;
using AuthService.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Text.Json.Serialization;

namespace AuthService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddControllers()
            .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
        services.AddEndpointsApiExplorer();

        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));

        services.AddDbContext<AuthDbContext>(opts =>
            opts.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection"),
                sql => sql.EnableRetryOnFailure(3)));

        services.AddStackExchangeRedisCache(opts =>
            opts.Configuration = configuration.GetConnectionString("Redis"));

        services.AddValidatorsFromAssemblyContaining<Program>();
        services.AddFluentValidationAutoValidation();

        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService,  AuthService.Services.AuthService>();
        services.AddScoped<IUserService,  UserService>();

        services.AddHealthChecks()
            .AddDbContextCheck<AuthDbContext>("database")
            .AddRedis(configuration.GetConnectionString("Redis")!, "redis");

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection(JwtOptions.SectionName);
        var key        = Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!);

        var logger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("JwtAuth");
        var keyFingerprint = Convert.ToBase64String(System.Security.Cryptography.SHA256.HashData(key));
        logger.LogInformation(
            "[JWT] Validation key fingerprint: {Fp} | SecretKey length: {Len} | Issuer: {Iss} | Audience: {Aud}",
            keyFingerprint, jwtSection["SecretKey"]?.Length ?? 0,
            jwtSection["Issuer"], jwtSection["Audience"]);

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(opts =>
            {
                // Force the legacy JwtSecurityTokenHandler for validation so it matches
                // the handler used in TokenService.GenerateAccessToken (JwtSecurityToken).
                // The new default JsonWebTokenHandler (NET 7+) rejects tokens with no 'kid'
                // header, which JwtSecurityTokenHandler does not emit.
                opts.UseSecurityTokenValidators = true;

                opts.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtSection["Issuer"],
                    ValidAudience            = jwtSection["Audience"],
                    IssuerSigningKey         = new SymmetricSecurityKey(key),
                    ClockSkew                = TimeSpan.Zero
                };

                opts.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        var log = ctx.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();
                        log.LogWarning(ctx.Exception,
                            "[JWT] Authentication failed: {Type} — {Message}",
                            ctx.Exception.GetType().Name, ctx.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

        services.AddAuthorization();
        return services;
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title       = "COG Auth Service API",
                Version     = "v1",
                Description = "Authentication, authorization, and user management"
            });

            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description  = "Paste your JWT access token (obtained from POST /api/auth/login). Do NOT include the 'Bearer ' prefix — Swagger adds it automatically.",
                Name         = "Authorization",
                In           = ParameterLocation.Header,
                Type         = SecuritySchemeType.Http,
                Scheme       = "bearer",
                BearerFormat = "JWT"
            });

            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
                    },
                    Array.Empty<string>()
                }
            });
        });

        return services;
    }
}
