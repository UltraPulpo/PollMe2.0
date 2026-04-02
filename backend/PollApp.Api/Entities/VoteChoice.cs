namespace PollApp.Api.Entities;

public class VoteChoice
{
    public Guid Id { get; set; }
    public Guid VoteId { get; set; }
    public Guid PollOptionId { get; set; }
}
