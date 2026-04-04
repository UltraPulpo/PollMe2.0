using Dapper;
using Microsoft.Data.Sqlite;
using PollApp.Api.Entities;

namespace PollApp.Api.Repositories;

public class PollRepository : IPollRepository
{
    private readonly string _connectionString;

    public PollRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")!;
    }

    public async Task CreateAsync(Poll poll, List<PollOption> options)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        await connection.ExecuteAsync(
            """
            INSERT INTO Polls (Id, CreatorId, Title, Description, PollType, IsActive, CreatedAtUtc)
            VALUES (@Id, @CreatorId, @Title, @Description, @PollType, @IsActive, @CreatedAtUtc)
            """,
            new
            {
                Id = poll.Id,
                CreatorId = poll.CreatorId,
                poll.Title,
                poll.Description,
                PollType = (int)poll.PollType,
                IsActive = poll.IsActive ? 1 : 0,
                CreatedAtUtc = poll.CreatedAtUtc.ToString("O")
            },
            transaction);

        foreach (var option in options)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO PollOptions (Id, PollId, Text, DisplayOrder)
                VALUES (@Id, @PollId, @Text, @DisplayOrder)
                """,
                new
                {
                    Id = option.Id,
                    PollId = option.PollId,
                    option.Text,
                    option.DisplayOrder
                },
                transaction);
        }

        await transaction.CommitAsync();
    }

    public async Task<Poll?> GetByIdAsync(Guid id)
    {
        using var connection = new SqliteConnection(_connectionString);
        return await connection.QuerySingleOrDefaultAsync<Poll>(
            "SELECT * FROM Polls WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<(Poll Poll, List<PollOption> Options)?> GetWithOptionsAsync(Guid id)
    {
        using var connection = new SqliteConnection(_connectionString);
        var poll = await connection.QuerySingleOrDefaultAsync<Poll>(
            "SELECT * FROM Polls WHERE Id = @Id",
            new { Id = id });

        if (poll is null)
            return null;

        var options = (await connection.QueryAsync<PollOption>(
            "SELECT * FROM PollOptions WHERE PollId = @PollId ORDER BY DisplayOrder",
            new { PollId = id })).ToList();

        return (poll, options);
    }

    public async Task<List<Poll>> GetByCreatorIdAsync(Guid creatorId)
    {
        using var connection = new SqliteConnection(_connectionString);
        var polls = await connection.QueryAsync<Poll>(
            "SELECT * FROM Polls WHERE CreatorId = @CreatorId ORDER BY CreatedAtUtc DESC",
            new { CreatorId = creatorId });
        return polls.ToList();
    }

    public async Task UpdateIsActiveAsync(Guid id, bool isActive)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.ExecuteAsync(
            "UPDATE Polls SET IsActive = @IsActive WHERE Id = @Id",
            new { Id = id, IsActive = isActive ? 1 : 0 });
    }

    public async Task DeleteAsync(Guid id)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        // Delete in reverse FK order: VoteChoices → Votes → PollOptions → Poll
        await connection.ExecuteAsync(
            """
            DELETE FROM VoteChoices WHERE VoteId IN (
                SELECT Id FROM Votes WHERE PollId = @Id
            )
            """,
            new { Id = id }, transaction);

        await connection.ExecuteAsync(
            "DELETE FROM Votes WHERE PollId = @Id",
            new { Id = id }, transaction);

        await connection.ExecuteAsync(
            "DELETE FROM PollOptions WHERE PollId = @Id",
            new { Id = id }, transaction);

        await connection.ExecuteAsync(
            "DELETE FROM Polls WHERE Id = @Id",
            new { Id = id }, transaction);

        await transaction.CommitAsync();
    }
}
