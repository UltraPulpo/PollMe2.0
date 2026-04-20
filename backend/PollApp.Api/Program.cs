using System.Text.Json.Serialization;
using Dapper;
using FluentMigrator.Runner;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using PollApp.Api.Hubs;
using PollApp.Api.Repositories;
using PollApp.Api.Services;
using PollApp.Api.Telemetry;

// Register Dapper type handler for Guid ↔ SQLite TEXT conversion
SqlMapper.AddTypeHandler(new GuidTypeHandler());

var builder = WebApplication.CreateBuilder(args);

// Ensure a shared fallback connection string is available to all consumers
// of IConfiguration, not just FluentMigrator.
var defaultConnection = builder.Configuration.GetConnectionString("DefaultConnection");
var usingFallbackConnection = string.IsNullOrWhiteSpace(defaultConnection);
if (usingFallbackConnection)
{
    builder.Configuration["ConnectionStrings:DefaultConnection"] = "Data Source=pollapp.db;Cache=Shared;";
}

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddProblemDetails();

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

// Register FluentMigrator — scans this assembly for Migration classes.
// The connection string is resolved lazily from IConfiguration so that
// integration tests can override it via ConfigureAppConfiguration.
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddSQLite()
        .WithGlobalConnectionString(sp =>
            sp.GetRequiredService<IConfiguration>().GetConnectionString("DefaultConnection")
            ?? "Data Source=pollapp.db;Cache=Shared;")
        .ScanIn(typeof(Program).Assembly).For.Migrations())
    .AddLogging(lb => lb.AddFluentMigratorConsole());

// Register repositories
builder.Services.AddScoped<IPollRepository, PollRepository>();
builder.Services.AddScoped<IVoteRepository, VoteRepository>();
builder.Services.AddScoped<ICreatorRepository, CreatorRepository>();

// Register services
builder.Services.AddScoped<ICreatorAuthService, CreatorAuthService>();

// Register SignalR for real-time updates
builder.Services.AddSignalR();

// Configure OpenTelemetry tracing and metrics
builder.Services.AddOpenTelemetry()
    .ConfigureResource(resource => resource.AddService(DiagnosticsConfig.ServiceName))
    .WithTracing(tracing =>
    {
        tracing
            .AddSource(DiagnosticsConfig.ServiceName)   // listen to our custom ActivitySource
            .AddAspNetCoreInstrumentation()             // auto-trace every HTTP request
            .AddHttpClientInstrumentation();            // auto-trace outgoing HTTP calls (if any)

        if (builder.Environment.IsDevelopment())
        {
            tracing.AddConsoleExporter();               // print traces to console for local dev
        }
    })
    .WithMetrics(metrics =>
    {
        metrics
            .AddMeter(DiagnosticsConfig.ServiceName)    // listen to our custom Meter
            .AddAspNetCoreInstrumentation();            // auto-collect HTTP metrics

        if (builder.Environment.IsDevelopment())
        {
            metrics.AddConsoleExporter();               // print metrics to console for local dev
        }
    });

var app = builder.Build();

if (usingFallbackConnection)
{
    app.Logger.LogWarning("Connection string 'DefaultConnection' not found. Using fallback SQLite database 'pollapp.db'.");
}

// Run all pending migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}

app.UseExceptionHandler(errorApp =>
{
    errorApp.Run(async context =>
    {
        await Results.Problem(statusCode: 500, title: "Internal Server Error").ExecuteAsync(context);
    });
});
app.UseStatusCodePages();

app.UseCors();

app.MapGet("/", () => Results.Ok(new { status = "healthy" }));

app.MapControllers();

// Map SignalR hub endpoint — clients connect via /hubs/poll
app.MapHub<PollHub>("/hubs/poll");

app.Run();

// Required for WebApplicationFactory<Program> in integration tests
public partial class Program { }
