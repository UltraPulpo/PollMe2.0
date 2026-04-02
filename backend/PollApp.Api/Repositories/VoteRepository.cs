using Dapper;
using Microsoft.Data.Sqlite;
using PollApp.Api.Entities;

namespace PollApp.Api.Repositories;

public class VoteRepository : IVoteRepository
{
    private readonly string _connectionString;

    public VoteRepository(IConfiguration config)
    {
        _connectionString = config.GetConnectionString("DefaultConnection")!;
    }

    public async Task CreateVoteAsync(Vote vote, List<VoteChoice> choices)
    {
        using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync();
        using var transaction = await connection.BeginTransactionAsync();

        await connection.ExecuteAsync(
            """
            INSERT INTO Votes (Id, PollId, VoterToken, CreatedAtUtc)
            VALUES (@Id, @PollId, @VoterToken, @CreatedAtUtc)
            """,
            new
            {
                Id = vote.Id,
                PollId = vote.PollId,
                vote.VoterToken,
                CreatedAtUtc = vote.CreatedAtUtc.ToString("O")
            },
            transaction);

        foreach (var choice in choices)
        {
            await connection.ExecuteAsync(
                """
                INSERT INTO VoteChoices (Id, VoteId, PollOptionId)
                VALUES (@Id, @VoteId, @PollOptionId)
                """,
                new
                {
                    Id = choice.Id,
                    VoteId = choice.VoteId,
                    PollOptionId = choice.PollOptionId
                },
                transaction);
        }

        await transaction.CommitAsync();
    }

    public async Task<bool> HasVotedAsync(Guid pollId, string voterToken)
    {
        using var connection = new SqliteConnection(_connectionString);
        var count = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Votes WHERE PollId = @PollId AND VoterToken = @VoterToken",
            new { PollId = pollId, VoterToken = voterToken });
        return count > 0;
    }

    public async Task<List<PollOptionResult>> GetResultsAsync(Guid pollId)
    {
        using var connection = new SqliteConnection(_connectionString);
        var results = await connection.QueryAsync<PollOptionResult>(
            """
            SELECT po.Id AS PollOptionId, po.Text, COUNT(vc.Id) AS VoteCount
            FROM PollOptions po
            LEFT JOIN VoteChoices vc ON vc.PollOptionId = po.Id
            WHERE po.PollId = @PollId
            GROUP BY po.Id, po.Text
            ORDER BY po.DisplayOrder
            """,
            new { PollId = pollId });
        return results.ToList();
    }
}
