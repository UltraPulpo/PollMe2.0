using PollApp.Api.Entities;

namespace PollApp.Api.Repositories;

public interface IVoteRepository
{
    Task CreateVoteAsync(Vote vote, List<VoteChoice> choices);
    Task<bool> HasVotedAsync(Guid pollId, string voterToken);
    Task<List<PollOptionResult>> GetResultsAsync(Guid pollId);
    Task<int> GetVoteCountAsync(Guid pollId);
}

public class PollOptionResult
{
    public Guid PollOptionId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int VoteCount { get; set; }
}
