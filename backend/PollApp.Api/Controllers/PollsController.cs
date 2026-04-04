using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using PollApp.Api.DTOs;
using PollApp.Api.Entities;
using PollApp.Api.Filters;
using PollApp.Api.Helpers;
using PollApp.Api.Hubs;
using PollApp.Api.Repositories;
using PollApp.Api.Services;

namespace PollApp.Api.Controllers;

[ApiController]
[Route("api/polls")]
public class PollsController : ControllerBase
{
    private readonly IPollRepository _pollRepository;
    private readonly IVoteRepository _voteRepository;
    private readonly ICreatorAuthService _creatorAuthService;
    private readonly IHubContext<PollHub> _hubContext;

    public PollsController(
        IPollRepository pollRepository,
        IVoteRepository voteRepository,
        ICreatorAuthService creatorAuthService,
        IHubContext<PollHub> hubContext)
    {
        _pollRepository = pollRepository;
        _voteRepository = voteRepository;
        _creatorAuthService = creatorAuthService;
        _hubContext = hubContext;
    }

    // POST /api/polls — Create a new poll (auto-creates creator if needed)
    [HttpPost]
    public async Task<IActionResult> CreatePoll([FromBody] CreatePollRequest request)
    {
        // Resolve existing creator from cookie, or create a new one
        var creator = await _creatorAuthService.GetCurrentCreatorAsync(HttpContext)
                      ?? await _creatorAuthService.CreateCreatorAsync(HttpContext);

        var poll = new Poll
        {
            Id = Guid.NewGuid(),
            CreatorId = creator.Id,
            Title = request.Title,
            Description = request.Description,
            PollType = request.PollType,
            IsActive = true,
            CreatedAtUtc = DateTime.UtcNow
        };

        var options = request.Options.Select((text, index) => new PollOption
        {
            Id = Guid.NewGuid(),
            PollId = poll.Id,
            Text = text,
            DisplayOrder = index
        }).ToList();

        await _pollRepository.CreateAsync(poll, options);

        var response = new CreatePollResponse
        {
            PollId = poll.Id,
            SecretToken = creator.SecretToken,
            VoteUrl = $"/poll/{poll.Id}",
            DashboardUrl = $"/dashboard/{creator.SecretToken}"
        };

        return CreatedAtAction(nameof(GetPoll), new { pollId = poll.Id }, response);
    }

    // GET /api/polls/{pollId} — Get a poll with its options
    [HttpGet("{pollId}")]
    public async Task<IActionResult> GetPoll(Guid pollId)
    {
        var result = await _pollRepository.GetWithOptionsAsync(pollId);
        if (result is null)
            return NotFound();

        var (poll, options) = result.Value;

        var response = new PollResponse
        {
            Id = poll.Id,
            Title = poll.Title,
            Description = poll.Description,
            PollType = poll.PollType,
            IsActive = poll.IsActive,
            CreatedAtUtc = poll.CreatedAtUtc,
            Options = options.Select(o => new PollOptionResponse
            {
                Id = o.Id,
                Text = o.Text
            }).ToList()
        };

        return Ok(response);
    }

    // POST /api/polls/{pollId}/vote — Submit a vote
    [HttpPost("{pollId}/vote")]
    public async Task<IActionResult> Vote(Guid pollId, [FromBody] VoteRequest request)
    {
        // Fetch poll with options
        var result = await _pollRepository.GetWithOptionsAsync(pollId);
        if (result is null)
            return NotFound();

        var (poll, options) = result.Value;

        if (!poll.IsActive)
        {
            return Problem(
                statusCode: 400,
                title: "Poll Closed",
                detail: "This poll is no longer accepting votes.");
        }

        // Get or create voter token
        var voterToken = VoterTokenHelper.GetOrCreateVoterToken(HttpContext);

        // Check if already voted
        if (await _voteRepository.HasVotedAsync(pollId, voterToken))
        {
            return Problem(
                statusCode: 409,
                title: "Already Voted",
                detail: "You have already voted on this poll.");
        }

        // Validate optionIds — all must belong to this poll
        var validOptionIds = options.Select(o => o.Id).ToHashSet();
        var invalidIds = request.OptionIds.Where(id => !validOptionIds.Contains(id)).ToList();
        if (invalidIds.Count > 0)
        {
            return Problem(
                statusCode: 400,
                title: "Invalid Options",
                detail: "One or more selected options do not belong to this poll.");
        }

        // Single-choice: exactly 1 option
        if (poll.PollType == PollType.SingleChoice && request.OptionIds.Count != 1)
        {
            return Problem(
                statusCode: 400,
                title: "Invalid Vote",
                detail: "Single-choice polls require exactly one option.");
        }

        // Multi-choice: at least 1 option
        if (poll.PollType == PollType.MultipleChoice && request.OptionIds.Count < 1)
        {
            return Problem(
                statusCode: 400,
                title: "Invalid Vote",
                detail: "Multiple-choice polls require at least one option.");
        }

        var vote = new Vote
        {
            Id = Guid.NewGuid(),
            PollId = pollId,
            VoterToken = voterToken,
            CreatedAtUtc = DateTime.UtcNow
        };

        var choices = request.OptionIds.Select(optionId => new VoteChoice
        {
            Id = Guid.NewGuid(),
            VoteId = vote.Id,
            PollOptionId = optionId
        }).ToList();

        await _voteRepository.CreateVoteAsync(vote, choices);

        // Broadcast updated results to all clients viewing this poll's results page
        var results = await _voteRepository.GetResultsAsync(pollId);
        var totalVotes = results.Sum(o => o.VoteCount);
        var broadcastPayload = new PollResultsResponse
        {
            PollId = pollId,
            Title = poll.Title,
            TotalVotes = totalVotes,
            Options = results.Select(o => new PollOptionResultResponse
            {
                Id = o.PollOptionId,
                Text = o.Text,
                VoteCount = o.VoteCount,
                Percentage = totalVotes > 0
                    ? Math.Round((double)o.VoteCount / totalVotes * 100, 1)
                    : 0
            }).ToList()
        };
        await _hubContext.Clients.Group(pollId.ToString())
            .SendAsync("ResultsUpdated", broadcastPayload);

        return NoContent();
    }

    // GET /api/polls/{pollId}/results — Get aggregated results
    [HttpGet("{pollId}/results")]
    public async Task<IActionResult> GetResults(Guid pollId)
    {
        var poll = await _pollRepository.GetByIdAsync(pollId);
        if (poll is null)
            return NotFound();

        var optionResults = await _voteRepository.GetResultsAsync(pollId);
        var totalVotes = optionResults.Sum(o => o.VoteCount);

        var response = new PollResultsResponse
        {
            PollId = poll.Id,
            Title = poll.Title,
            TotalVotes = totalVotes,
            Options = optionResults.Select(o => new PollOptionResultResponse
            {
                Id = o.PollOptionId,
                Text = o.Text,
                VoteCount = o.VoteCount,
                Percentage = totalVotes > 0
                    ? Math.Round((double)o.VoteCount / totalVotes * 100, 1)
                    : 0
            }).ToList()
        };

        return Ok(response);
    }

    // PATCH /api/polls/{pollId} — Toggle IsActive (creator required)
    [HttpPatch("{pollId}")]
    [CreatorRequired]
    public async Task<IActionResult> TogglePollActive(Guid pollId)
    {
        var poll = await _pollRepository.GetByIdAsync(pollId);
        if (poll is null)
            return NotFound();

        // Verify ownership
        var creator = (Creator)HttpContext.Items["Creator"]!;
        if (poll.CreatorId != creator.Id)
        {
            return Problem(
                statusCode: 403,
                title: "Forbidden",
                detail: "You do not own this poll.");
        }

        var newIsActive = !poll.IsActive;
        await _pollRepository.UpdateIsActiveAsync(pollId, newIsActive);

        // Return updated poll
        var result = await _pollRepository.GetWithOptionsAsync(pollId);
        var (updatedPoll, options) = result!.Value;

        var response = new PollResponse
        {
            Id = updatedPoll.Id,
            Title = updatedPoll.Title,
            Description = updatedPoll.Description,
            PollType = updatedPoll.PollType,
            IsActive = updatedPoll.IsActive,
            CreatedAtUtc = updatedPoll.CreatedAtUtc,
            Options = options.Select(o => new PollOptionResponse
            {
                Id = o.Id,
                Text = o.Text
            }).ToList()
        };

        return Ok(response);
    }

    // DELETE /api/polls/{pollId} — Delete a poll (creator required)
    [HttpDelete("{pollId}")]
    [CreatorRequired]
    public async Task<IActionResult> DeletePoll(Guid pollId)
    {
        var poll = await _pollRepository.GetByIdAsync(pollId);
        if (poll is null)
            return NotFound();

        // Verify ownership
        var creator = (Creator)HttpContext.Items["Creator"]!;
        if (poll.CreatorId != creator.Id)
        {
            return Problem(
                statusCode: 403,
                title: "Forbidden",
                detail: "You do not own this poll.");
        }

        await _pollRepository.DeleteAsync(pollId);
        return NoContent();
    }
}
