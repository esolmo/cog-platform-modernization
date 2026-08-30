using CasinoService.Configuration;
using CasinoService.Data;
using CasinoService.Services;
using FluentValidation;
using FluentValidation.AspNetCore;
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

Log.Information("=== Casino service starting ===");

try
{
    var builder = WebApplication.CreateBuilder(args);

    builder.Host.UseSerilog((ctx, services, config) =>
        config.ReadFrom.Configuration(ctx.Configuration)
              .ReadFrom.Services(services)
              .Enrich.FromLogContext()
              .Enrich.WithMachineName()
              .Enrich.WithThreadId()
              .WriteTo.Console(outputTemplate:
                  "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
              .WriteTo.File("logs/casino-service-.log", rollingInterval: RollingInterval.Day));

    // ── Configuration ──────────────────────────────────────────────────────
    builder.Services.Configure<CasinoOptions>(
        builder.Configuration.GetSection(CasinoOptions.Section));

    // ── Database ───────────────────────────────────────────────────────────
    builder.Services.AddDbContext<CasinoDbContext>(opts =>
        opts.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(3))
        .ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));

    // ── HTTP clients ───────────────────────────────────────────────────────
    builder.Services.AddHttpClient<ILiveDealerClient, LiveDealerXmlClient>(c =>
    {
        c.Timeout = TimeSpan.FromSeconds(30);
    });

    builder.Services.AddHttpClient<IAccountsClient, AccountsHttpClient>(c =>
    {
        c.BaseAddress = new Uri(
            builder.Configuration["Services:Accounts"] ?? "http://accounts-service");
        c.Timeout = TimeSpan.FromSeconds(10);
    });

    // ── Application services ───────────────────────────────────────────────
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICasinoService, CasinoService.Services.CasinoService>();
    builder.Services.AddValidatorsFromAssemblyContaining<Program>();
    builder.Services.AddFluentValidationAutoValidation();

    // ── JWT authentication ─────────────────────────────────────────────────
    var jwtSection = builder.Configuration.GetSection("Jwt");
    var key        = Encoding.UTF8.GetBytes(jwtSection["SecretKey"]!);
    var signingKey = new SymmetricSecurityKey(key);

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(opts =>
        {
            opts.UseSecurityTokenValidators = true;
            opts.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey         = signingKey,
                IssuerSigningKeyResolver = (_, _, _, _) => [signingKey],
                ValidateIssuer           = true,
                ValidIssuer              = jwtSection["Issuer"],
                ValidateAudience         = true,
                ValidAudience            = jwtSection["Audience"],
                ClockSkew                = TimeSpan.FromSeconds(30)
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

    // ── Health checks ──────────────────────────────────────────────────────
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<CasinoDbContext>("database");

    // ── Swagger ────────────────────────────────────────────────────────────
    builder.Services.AddControllers()
        .AddJsonOptions(o => o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerGen(c =>
    {
        c.SwaggerDoc("v1", new OpenApiInfo
        {
            Title       = "COG Casino Service",
            Version     = "v1",
            Description = "Live Dealer casino integration — player registration, sessions, and fund transfers"
        });
        c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description   = "JWT Authorization header. Paste the token only — Bearer prefix is added automatically.",
            Name          = "Authorization",
            In            = ParameterLocation.Header,
            Type          = SecuritySchemeType.Http,
            Scheme        = "bearer",
            BearerFormat  = "JWT"
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

    var app = builder.Build();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();

        Log.Information("[STARTUP] Dev — applying EF Core migrations...");
        Log.Information("[STARTUP] Connection string: {Cs}",
            MaskPassword(builder.Configuration.GetConnectionString("DefaultConnection")));
        try
        {
            using var scope = app.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CasinoDbContext>();
            Log.Information("[STARTUP] Applying pending migrations (will create DB if absent)...");
            await db.Database.MigrateAsync();
            Log.Information("[STARTUP] Migrations complete.");
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[STARTUP] Migration failed — continuing.");
        }
    }

    app.UseSerilogRequestLogging();
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health/live");
    app.MapHealthChecks("/health/ready");

    Log.Information("[STARTUP] Casino service ready.");
    await app.RunAsync();
}
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Casino service terminated unexpectedly");
    return 1;
}
finally
{
    await Log.CloseAndFlushAsync();
}

return 0;

static string MaskPassword(string? cs)
{
    if (string.IsNullOrEmpty(cs)) return "(empty)";
    return System.Text.RegularExpressions.Regex.Replace(
        cs, @"(Password|PWD)=[^;]*", "$1=***", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
}

// Exposes Program to WebApplicationFactory<Program> for integration tests.
public partial class Program { }
