using Microsoft.AspNetCore.Mvc;
using PollApp.Api.DTOs;
using PollApp.Api.Repositories;

namespace PollApp.Api.Controllers;

[ApiController]
[Route("api/creator")]
public class CreatorController : ControllerBase
{
    private readonly ICreatorRepository _creatorRepository;
    private readonly IPollRepository _pollRepository;
    private readonly IVoteRepository _voteRepository;

    public CreatorController(
        ICreatorRepository creatorRepository,
        IPollRepository pollRepository,
        IVoteRepository voteRepository)
    {
        _creatorRepository = creatorRepository;
        _pollRepository = pollRepository;
        _voteRepository = voteRepository;
    }

    // GET /api/creator/{secretToken}/polls — Get all polls for a creator (magic link)
    [HttpGet("{secretToken}/polls")]
    public async Task<IActionResult> GetCreatorPolls(Guid secretToken)
    {
        var creator = await _creatorRepository.GetBySecretTokenAsync(secretToken);
        if (creator is null)
            return NotFound();

        var polls = await _pollRepository.GetByCreatorIdAsync(creator.Id);

        var summaries = new List<CreatorPollSummary>();
        foreach (var poll in polls)
        {
            var totalVotes = await _voteRepository.GetVoteCountAsync(poll.Id);
            summaries.Add(new CreatorPollSummary
            {
                Id = poll.Id,
                Title = poll.Title,
                PollType = poll.PollType,
                TotalVotes = totalVotes,
                IsActive = poll.IsActive,
                CreatedAtUtc = poll.CreatedAtUtc
            });
        }

        return Ok(summaries);
    }
}
