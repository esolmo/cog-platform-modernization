using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using ReportsService.Services;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

Log.Information("=== {ServiceName} service starting ===", "Reports");

try
{
    Log.Information("[STARTUP] Creating WebApplication builder...");
    var builder = WebApplication.CreateBuilder(args);

    Log.Information("[STARTUP] Configuring Serilog from app configuration...");
    builder.Host.UseSerilog((ctx, services, cfg) =>
        cfg.ReadFrom.Configuration(ctx.Configuration)
           .ReadFrom.Services(services)
           .Enrich.FromLogContext()
           .WriteTo.Console(outputTemplate:
               "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
           .WriteTo.File("logs/reports-service-.log", rollingInterval: RollingInterval.Day));

    Log.Information("[STARTUP] Registering services...");

    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var reportsSigningKey = new SymmetricSecurityKey(
        Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!));

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            // UseSecurityTokenValidators = true forces legacy JwtSecurityTokenHandler.
            // Fixes IDX10517 when tokens have no 'kid' header (default from JwtSecurityToken).
            opts.UseSecurityTokenValidators = true;

            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtSettings["Issuer"],
                ValidAudience = jwtSettings["Audience"],
                IssuerSigningKey = reportsSigningKey,
                IssuerSigningKeyResolver = (_, _, _, _) => [reportsSigningKey]
            };
            opts.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    Log.Warning("[JWT] Authentication failed: {Message}", ctx.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization(opts =>
    {
        opts.AddPolicy("CanViewReports", policy =>
            policy.RequireClaim("permission", "Reports.View", "Reports.Export"));
    });

    builder.Services.AddScoped<IReportsService, ReportsDataService>();
    builder.Services.AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Reports Service", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Name = "Authorization",
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT",
            In = ParameterLocation.Header,
            Description = "JWT Authorization header. Paste the token only — Bearer prefix is added automatically."
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddCors(opts =>
        opts.AddDefaultPolicy(policy =>
            policy.WithOrigins(builder.Configuration.GetSection("AllowedOrigins").Get<string[]>() ?? [])
                  .AllowAnyHeader().AllowAnyMethod()));

    builder.Services.AddHealthChecks();

    Log.Information("[STARTUP] Services registered.");

    Log.Information("[STARTUP] Building WebApplication...");
    var app = builder.Build();
    Log.Information("[STARTUP] WebApplication built successfully.");

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    Log.Information("[STARTUP] Configuring middleware pipeline...");
    app.UseSerilogRequestLogging();
    app.UseCors();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health/live");
    app.MapHealthChecks("/health/ready");
    Log.Information("[STARTUP] Middleware pipeline configured.");

    Log.Information("[STARTUP] Starting Kestrel and listening for requests...");
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Reports service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }
