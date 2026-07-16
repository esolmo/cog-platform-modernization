using System.Text.Json.Serialization;
using AccountsService.Data;
using AccountsService.Extensions;
using Microsoft.EntityFrameworkCore;
using Serilog;
using Serilog.Events;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console(outputTemplate:
        "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateBootstrapLogger();

Log.Information("=== Accounts service starting ===");

try
{
    Log.Information("[STARTUP] Creating WebApplication builder...");
    var builder = WebApplication.CreateBuilder(args);

    Log.Information("[STARTUP] Configuring Serilog from app configuration...");
    builder.Host.UseSerilog((context, services, config) =>
        config
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .Enrich.WithMachineName()
            .Enrich.WithThreadId()
            .WriteTo.Console(outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] {SourceContext}: {Message:lj}{NewLine}{Exception}")
            .WriteTo.File("logs/accounts-service-.log", rollingInterval: RollingInterval.Day));

    Log.Information("[STARTUP] Registering DbContext (SQL Server)...");
    builder.Services.AddDbContext<AccountsDbContext>(options =>
        options.UseSqlServer(
            builder.Configuration.GetConnectionString("DefaultConnection"),
            sql => sql.EnableRetryOnFailure(maxRetryCount: 5))
        .ConfigureWarnings(w =>
            w.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.RelationalEventId.PendingModelChangesWarning)));
    Log.Information("[STARTUP] DbContext registered.");

    Log.Information("[STARTUP] Registering application services...");
    builder.Services.AddApplicationServices();
    builder.Services.AddServiceClients(builder.Configuration);
    builder.Services.AddJwtAuthentication(builder.Configuration);
    builder.Services.AddAuthorization();
    builder.Services.AddControllers()
        .AddJsonOptions(o =>
            o.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
    builder.Services.AddEndpointsApiExplorer();
    builder.Services.AddSwaggerDocumentation();
    builder.Services.AddHealthChecks()
        .AddDbContextCheck<AccountsDbContext>();
    Log.Information("[STARTUP] Services registered.");

    Log.Information("[STARTUP] Building WebApplication (DI container)...");
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
            var db = scope.ServiceProvider.GetRequiredService<AccountsDbContext>();

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
    app.UseAuthentication();
    app.UseAuthorization();
    app.MapControllers();
    app.MapHealthChecks("/health/live");
    app.MapHealthChecks("/health/ready");
    Log.Information("[STARTUP] Middleware pipeline configured.");

    Log.Information("[STARTUP] Starting Kestrel and listening for requests...");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "accounts-service failed to start");
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
