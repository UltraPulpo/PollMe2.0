using System.ComponentModel.DataAnnotations;

namespace PollApp.Api.DTOs;

public class VoteRequest
{
    [Required, MinLength(1)]
    public List<Guid> OptionIds { get; set; } = new();
}
