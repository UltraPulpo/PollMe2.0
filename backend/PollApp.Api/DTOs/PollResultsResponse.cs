namespace PollApp.Api.DTOs;

public class PollResultsResponse
{
    public Guid PollId { get; set; }
    public string Title { get; set; } = string.Empty;
    public int TotalVotes { get; set; }
    public List<PollOptionResultResponse> Options { get; set; } = new();
}

public class PollOptionResultResponse
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
    public int VoteCount { get; set; }
    public double Percentage { get; set; }
}
