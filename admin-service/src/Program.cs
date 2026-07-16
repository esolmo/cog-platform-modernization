using AdminService.Data;
using AdminService.Services;
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

Log.Information("=== {ServiceName} service starting ===", "Admin");

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
           .WriteTo.File("logs/admin-.log", rollingInterval: RollingInterval.Day));

    Log.Information("[STARTUP] Registering services...");

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "COG Admin Service", Version = "v1" });
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

    builder.Services.AddDbContext<AdminDbContext>(options =>
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
            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = ctx =>
                {
                    Log.Warning("[JWT] Authentication failed: {Message}", ctx.Exception.Message);
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization(options =>
    {
        options.AddPolicy("CanViewUsers",       policy => policy.RequireClaim("permission", "Users.Manage",  "Roles.Manage"));
        options.AddPolicy("CanManageUsers",     policy => policy.RequireClaim("permission", "Users.Manage"));
        options.AddPolicy("CanManageRoles",     policy => policy.RequireClaim("permission", "Roles.Manage"));
        options.AddPolicy("CanViewConfig",      policy => policy.RequireClaim("permission", "System.Config"));
        options.AddPolicy("CanEditConfig",      policy => policy.RequireClaim("permission", "System.Config"));
        options.AddPolicy("CanViewAuditLogs",   policy => policy.RequireClaim("permission", "Users.Manage",  "System.Config"));
    });

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddHttpClient("auth-service", client =>
    {
        client.BaseAddress = new Uri(
            builder.Configuration["Services:AuthService:BaseUrl"] ?? "http://localhost:5010");
        client.Timeout = TimeSpan.FromSeconds(15);
    });
    builder.Services.AddSingleton<IAuthProvisioningService, AuthProvisioningService>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IRoleService, RoleService>();
    builder.Services.AddScoped<ISystemConfigService, SystemConfigService>();
    builder.Services.AddScoped<IAuditService, AuditService>();

    builder.Services.AddCors(options =>
        options.AddPolicy("AllowFrontend", policy =>
            policy.WithOrigins(
                builder.Configuration.GetSection("AllowedOrigins").Get<string[]>()
                    ?? new[] { "http://localhost:5173", "http://localhost:5175" })
                  .AllowAnyHeader()
                  .AllowAnyMethod()
                  .AllowCredentials()));

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AdminDbContext>("db");

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
            var db = scope.ServiceProvider.GetRequiredService<AdminDbContext>();
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
    app.MapHealthChecks("/health/live");
    app.MapHealthChecks("/health/ready");
    Log.Information("[STARTUP] Middleware pipeline configured.");

    Log.Information("[STARTUP] Starting Kestrel and listening for requests...");
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Admin service terminated unexpectedly");
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
