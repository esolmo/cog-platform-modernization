using LotteryService.Data;
using LotteryService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Serilog;
using System.Text;
using System.Text.Json.Serialization;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

Log.Information("=== {ServiceName} service starting ===", "Lottery");

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
           .WriteTo.File("logs/lottery-service-.log", rollingInterval: RollingInterval.Day));

    Log.Information("[STARTUP] Registering services...");

    builder.Services.AddDbContext<LotteryDbContext>(opts =>
        opts.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(maxRetryCount: 5))
        .ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

    var jwtSettings = builder.Configuration.GetSection("Jwt");
    var lotterySigningKey = new SymmetricSecurityKey(
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
                IssuerSigningKey = lotterySigningKey,
                IssuerSigningKeyResolver = (_, _, _, _) => [lotterySigningKey]
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

    builder.Services.AddAuthorization();

    builder.Services.AddHttpContextAccessor();
    builder.Services.AddHttpClient<IAccountsClient, AccountsClient>(client =>
    {
        client.BaseAddress = new Uri(builder.Configuration["Services:Accounts"] ?? "http://accounts-service:5020");
    });

    builder.Services.AddScoped<ILotteryService, LotteryGameService>();
    builder.Services.AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo { Title = "Lottery Service", Version = "v1" });
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

    builder.Services.AddHealthChecks()
        .AddDbContextCheck<LotteryDbContext>();

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
            var db = scope.ServiceProvider.GetRequiredService<LotteryDbContext>();

            // If tables were created by T-SQL Phase 6 scripts, seed __EFMigrationsHistory
            // so EF skips InitialCreate and doesn't try to recreate existing tables.
            // Only relevant when the database already exists — on a brand-new database this
            // raw SQL would fail (login error) before MigrateAsync() gets a chance to create it.
            Log.Information("[STARTUP] Ensuring migration history is seeded...");
            if (await db.Database.CanConnectAsync())
            {
                await db.Database.ExecuteSqlRawAsync("""
                    IF NOT EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                                   WHERE TABLE_NAME = '__EFMigrationsHistory')
                    BEGIN
                        CREATE TABLE [__EFMigrationsHistory] (
                            [MigrationId]    nvarchar(150) NOT NULL,
                            [ProductVersion] nvarchar(32)  NOT NULL,
                            CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
                        );
                    END

                    IF NOT EXISTS (SELECT 1 FROM [__EFMigrationsHistory]
                                   WHERE [MigrationId] = '20260409120000_InitialCreate')
                        AND EXISTS (SELECT 1 FROM INFORMATION_SCHEMA.TABLES
                                    WHERE TABLE_NAME = 'LotteryGames')
                    BEGIN
                        INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
                        VALUES ('20260409120000_InitialCreate', '8.0.0');
                    END
                    """);
            }

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
    Log.Fatal(ex, "Lottery service terminated unexpectedly");
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

public partial class Program { }
