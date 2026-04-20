using System.Text.Json.Serialization;
using Dapper;
using FluentMigrator.Runner;
using PollApp.Api.Hubs;
using PollApp.Api.Repositories;
using PollApp.Api.Services;

// Register Dapper type handler for Guid ↔ SQLite TEXT conversion
SqlMapper.AddTypeHandler(new GuidTypeHandler());

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
if (string.IsNullOrWhiteSpace(connectionString))
{
    // Fallback to a local SQLite database if no connection string is configured.
    // This prevents unexpected crashes when running via CLI without appsettings.
    connectionString = "Data Source=pollapp.db;Cache=Shared;";
    Console.WriteLine("Warning: Connection string 'DefaultConnection' not found. Using fallback SQLite database 'pollapp.db'.");
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

// Register FluentMigrator — scans this assembly for Migration classes
builder.Services.AddFluentMigratorCore()
    .ConfigureRunner(rb => rb
        .AddSQLite()
        .WithGlobalConnectionString(connectionString)
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

var app = builder.Build();

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
