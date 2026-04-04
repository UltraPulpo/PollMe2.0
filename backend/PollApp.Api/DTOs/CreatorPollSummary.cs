using PollApp.Api.Entities;

namespace PollApp.Api.DTOs;

public class CreatorPollSummary
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public PollType PollType { get; set; }
    public int TotalVotes { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
