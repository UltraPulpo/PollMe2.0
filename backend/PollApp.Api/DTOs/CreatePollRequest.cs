using System.ComponentModel.DataAnnotations;
using PollApp.Api.Entities;

namespace PollApp.Api.DTOs;

public class CreatePollRequest
{
    [Required, MaxLength(200)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(1000)]
    public string? Description { get; set; }

    [Required]
    public PollType PollType { get; set; }

    [Required, MinLength(2), MaxLength(20)]
    public List<string> Options { get; set; } = new();
}
