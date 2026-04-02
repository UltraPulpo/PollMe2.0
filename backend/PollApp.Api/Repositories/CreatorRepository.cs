using Dapper;
using Microsoft.Data.Sqlite;
using PollApp.Api.Entities;

namespace PollApp.Api.Repositories;

public class CreatorRepository : ICreatorRepository
{
    private readonly string _connectionString;

    public CreatorRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")!;
    }

    public async Task CreateAsync(Creator creator)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(
            """
            INSERT INTO Creators (Id, SecretToken, DisplayName, CreatedAtUtc)
            VALUES (@Id, @SecretToken, @DisplayName, @CreatedAtUtc)
            """,
            new
            {
                Id = creator.Id,
                SecretToken = creator.SecretToken,
                creator.DisplayName,
                CreatedAtUtc = creator.CreatedAtUtc.ToString("O")
            });
    }

    public async Task<Creator?> GetBySecretTokenAsync(Guid secretToken)
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<Creator>(
            "SELECT * FROM Creators WHERE SecretToken = @SecretToken",
            new { SecretToken = secretToken });
    }
}
