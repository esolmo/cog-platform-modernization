using AlertsService.Data;
using AlertsService.Hubs;
using AlertsService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

Log.Information("=== {ServiceName} service starting ===", "Alerts");

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
           .WriteTo.File("logs/alerts-.log", rollingInterval: RollingInterval.Day));

    Log.Information("[STARTUP] Registering services...");

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "COG Alerts Service", Version = "v1" });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.Http,
            Scheme = "bearer",
            BearerFormat = "JWT"
        });
        c.AddSecurityRequirement(new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme { Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" } },
                Array.Empty<string>()
            }
        });
    });

    builder.Services.AddDbContext<AlertsDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(maxRetryCount: 5))
        .ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

    var jwtKey = builder.Configuration["Jwt:SecretKey"]
        ?? throw new InvalidOperationException("JWT key not configured");
    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // UseSecurityTokenValidators = true forces legacy JwtSecurityTokenHandler.
            // Fixes IDX10517 when tokens have no 'kid' header (default from JwtSecurityToken).
            options.UseSecurityTokenValidators = true;

            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                IssuerSigningKeyResolver = (_, _, _, _) => [signingKey],
                ValidateIssuer = true,
                ValidIssuer = builder.Configuration["Jwt:Issuer"],
                ValidateAudience = true,
                ValidAudience = builder.Configuration["Jwt:Audience"],
                ClockSkew = TimeSpan.Zero
            };
            // Allow JWT in query string for SignalR WebSocket connections
            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = ctx =>
                {
                    var accessToken = ctx.Request.Query["access_token"];
                    var path = ctx.HttpContext.Request.Path;
                    if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                        ctx.Token = accessToken;
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = ctx =>
                {
                    Log.Warning("[JWT] Authentication failed: {Message}", ctx.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();

    // SignalR with Redis backplane
    var redisConn = builder.Configuration.GetConnectionString("Redis");
    var signalR = builder.Services.AddSignalR();
    if (!string.IsNullOrEmpty(redisConn))
        signalR.AddStackExchangeRedis(redisConn);

    // Application services
    builder.Services.AddScoped<IAlertDataService, AlertDataService>();
    builder.Services.AddScoped<IAgentService, AgentCacheService>();
    builder.Services.AddScoped<IEmailService, EmailService>();
    builder.Services.AddStackExchangeRedisCache(options =>
    {
        options.Configuration = redisConn;
        options.InstanceName = "alerts:";
    });

    builder.Services.AddCors(options =>
        options.AddPolicy("AllowFrontend", policy =>
            policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:5173", "http://localhost:5174" })
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()));

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AlertsDbContext>("db");

    Log.Information("[STARTUP] Services registered.");

    Log.Information("[STARTUP] Building WebApplication...");
    var app = builder.Build();
    Log.Information("[STARTUP] WebApplication built successfully.");

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        Log.Information("[STARTUP] Dev environment — running EF Core migrations...");
        Log.Information("[STARTUP] Connection string (masked): {Cs}",
            MaskPassword(builder.Configuration.GetConnectionString("DefaultConnection")));
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AlertsDbContext>();
            Log.Information("[STARTUP] Applying pending migrations (will create DB if absent)...");
            await db.Database.MigrateAsync();
            Log.Information("[STARTUP] Migrations complete.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[STARTUP] Migration failed — continuing startup without migration.");
        }
    }

    Log.Information("[STARTUP] Configuring middleware pipeline...");
    app.UseSerilogRequestLogging();
    app.UseCors("AllowFrontend");
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHub<AlertHub>("/hubs/alerts");
    app.MapHealthChecks("/health/live");
    app.MapHealthChecks("/health/ready");
    Log.Information("[STARTUP] Middleware pipeline configured.");

    Log.Information("[STARTUP] Starting Kestrel and listening for requests...");
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Alerts service terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

static string MaskPassword(string? cs)
{
    if (string.IsNullOrEmpty(cs)) return "(empty)";
    return System.Text.RegularExpressions.Regex.Replace(
        cs, @"(Password|PWD)=[^;]*", "$1=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}
