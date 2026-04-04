using PollApp.Api.Entities;

namespace PollApp.Api.DTOs;

public class PollResponse
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public PollType PollType { get; set; }
    public bool IsActive { get; set; }
    public List<PollOptionResponse> Options { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
}

public class PollOptionResponse
{
    public Guid Id { get; set; }
    public string Text { get; set; } = string.Empty;
}
