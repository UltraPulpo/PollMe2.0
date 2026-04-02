namespace PollApp.Api.Entities;

public class Creator
{
    public Guid Id { get; set; }
    public Guid SecretToken { get; set; }
    public string? DisplayName { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
