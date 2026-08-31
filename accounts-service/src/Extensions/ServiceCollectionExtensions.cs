using System.Security.Cryptography;
using System.Text;
using AccountsService.Configuration;
using AccountsService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

namespace AccountsService.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IAgentService, AgentService>();
        services.AddScoped<ITransactionService, TransactionService>();
        services.AddSingleton<IBettingProvisioningService, BettingProvisioningService>();
        services.AddSingleton<IAuthProvisioningService, AuthProvisioningService>();
        return services;
    }

    public static IServiceCollection AddServiceClients(this IServiceCollection services, IConfiguration configuration)
    {
        var authBaseUrl     = configuration["Services:AuthService:BaseUrl"]    ?? "http://localhost:5010";
        var bettingBaseUrl  = configuration["Services:BettingService:BaseUrl"] ?? "http://localhost:5050";

        services.AddHttpClient("auth-service", client =>
        {
            client.BaseAddress = new Uri(authBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        services.AddHttpClient("betting-service", client =>
        {
            client.BaseAddress = new Uri(bettingBaseUrl);
            client.Timeout = TimeSpan.FromSeconds(15);
        });

        return services;
    }

    public static IServiceCollection AddJwtAuthentication(
        this IServiceCollection services, IConfiguration configuration)
    {
        var jwtOptions = configuration
            .GetSection(JwtOptions.SectionName)
            .Get<JwtOptions>()!;

        var keyBytes   = Encoding.UTF8.GetBytes(jwtOptions.SecretKey);
        var signingKey = new SymmetricSecurityKey(keyBytes);

        var fingerprint = Convert.ToBase64String(SHA256.HashData(keyBytes));
        var startupLogger = LoggerFactory.Create(b => b.AddConsole()).CreateLogger("AccountsService.JwtAuth");
        startupLogger.LogInformation(
            "[JWT] Validation key fingerprint: {Fp} | SecretKey length: {Len} | Issuer: {Iss} | Audience: {Aud}",
            fingerprint, jwtOptions.SecretKey.Length, jwtOptions.Issuer, jwtOptions.Audience);

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                // Force legacy JwtSecurityTokenHandler — tokens from auth-service are created
                // without a 'kid' header, which the new JsonWebTokenHandler (default in .NET 7+)
                // rejects with IDX10517. Both services must use the same handler.
                options.UseSecurityTokenValidators = true;

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer           = true,
                    ValidateAudience         = true,
                    ValidateLifetime         = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer              = jwtOptions.Issuer,
                    ValidAudience            = jwtOptions.Audience,
                    IssuerSigningKey         = signingKey,
                    // Resolver ensures the key is found even when the token has no 'kid' header,
                    // which is required for both JwtSecurityTokenHandler and JsonWebTokenHandler.
                    IssuerSigningKeyResolver = (_, _, _, _) => [signingKey],
                    ClockSkew = TimeSpan.FromSeconds(30)
                };

                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = ctx =>
                    {
                        var log = ctx.HttpContext.RequestServices
                            .GetRequiredService<ILogger<JwtBearerEvents>>();
                        log.LogWarning(ctx.Exception,
                            "[JWT] Authentication failed: {Message}", ctx.Exception.Message);
                        return Task.CompletedTask;
                    }
                };
            });

        return services;
    }

    public static IServiceCollection AddSwaggerDocumentation(this IServiceCollection services)
    {
        services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title   = "COG Accounts Service",
                Version = "v1",
                Description = "Customer accounts, balances, and financial transaction management"
            });
            c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Description = "JWT Authorization header. Format: Bearer {token}",
                Name        = "Authorization",
                In          = ParameterLocation.Header,
                Type        = SecuritySchemeType.Http,
                Scheme      = "bearer"
            });
            c.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id   = "Bearer"
                        }
                    },
                    []
                }
            });
        });
        return services;
    }
}
