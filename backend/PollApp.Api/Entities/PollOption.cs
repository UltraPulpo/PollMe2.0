namespace PollApp.Api.Entities;

public class PollOption
{
    public Guid Id { get; set; }
    public Guid PollId { get; set; }
    public string Text { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
}
