namespace PollApp.Api.DTOs;

public class CreatePollResponse
{
    public Guid PollId { get; set; }
    public Guid SecretToken { get; set; }
    public string VoteUrl { get; set; } = string.Empty;
    public string DashboardUrl { get; set; } = string.Empty;
}
