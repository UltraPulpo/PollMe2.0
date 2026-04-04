namespace PollApp.Api.Entities;

public class Vote
{
    public Guid Id { get; set; }
    public Guid PollId { get; set; }
    public string VoterToken { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
