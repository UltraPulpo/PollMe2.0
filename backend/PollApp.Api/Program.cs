using Dapper;
using FluentMigrator.Runner;
using PollApp.Api.Repositories;

// Register Dapper type handler for Guid ↔ SQLite TEXT conversion
SqlMapper.AddTypeHandler(new GuidTypeHandler());

var builder = WebApplication.CreateBuilder(args);

var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddControllers();

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

var app = builder.Build();

// Run all pending migrations automatically on startup
using (var scope = app.Services.CreateScope())
{
    var runner = scope.ServiceProvider.GetRequiredService<IMigrationRunner>();
    runner.MigrateUp();
}

app.UseCors();

app.MapGet("/", () => Results.Ok(new { status = "healthy" }));

app.MapControllers();

app.Run();
