// ============================================================
// xUnit <-> NUnit Quick Reference (for devs coming from NUnit)
// ============================================================
// xUnit [Fact]               = NUnit [Test]              — a single test case
// xUnit [Theory]+[InlineData] = NUnit [TestCase]          — parameterized test
// xUnit constructor           = NUnit [SetUp]             — runs before EACH test
//   (xUnit creates a NEW class instance for every test — no shared state by default!)
// xUnit IDisposable.Dispose   = NUnit [TearDown]          — runs after each test
// xUnit IAsyncLifetime        = NUnit async [SetUp]/[TearDown]
// xUnit IClassFixture<T>      = NUnit [OneTimeSetUp]      — shared per test class
// xUnit ICollectionFixture<T> = shared across multiple test classes
// xUnit has NO [TestFixture]  — the test class itself is the fixture
// xUnit has NO Assert.That()  — use FluentAssertions instead: result.Should().Be(expected)
// ============================================================

using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using NSubstitute;
using PollApp.Api.Controllers;
using PollApp.Api.DTOs;
using PollApp.Api.Entities;
using PollApp.Api.Hubs;
using PollApp.Api.Repositories;
using PollApp.Api.Services;

namespace PollApp.Api.Tests.Unit;

public class PollsControllerTests
{
    // ── Mocks ───────────────────────────────────────────────────────────────────
    private readonly IPollRepository _pollRepo;
    private readonly IVoteRepository _voteRepo;
    private readonly ICreatorAuthService _authService;
    private readonly IHubContext<PollHub> _hubContext;
    private readonly PollsController _controller;

    // xUnit constructor = NUnit [SetUp] — a fresh instance per test
    public PollsControllerTests()
    {
        _pollRepo = Substitute.For<IPollRepository>();
        _voteRepo = Substitute.For<IVoteRepository>();
        _authService = Substitute.For<ICreatorAuthService>();
        _hubContext = Substitute.For<IHubContext<PollHub>>();

        // Chain: hubContext.Clients.Group(...).SendCoreAsync(...)
        var hubClients = Substitute.For<IHubClients>();
        var clientProxy = Substitute.For<IClientProxy>();
        _hubContext.Clients.Returns(hubClients);
        hubClients.Group(Arg.Any<string>()).Returns(clientProxy);
        clientProxy.SendCoreAsync(Arg.Any<string>(), Arg.Any<object?[]>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        _controller = new PollsController(_pollRepo, _voteRepo, _authService, _hubContext)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static (Poll, List<PollOption>) MakePoll(PollType type = PollType.SingleChoice, bool isActive = true)
    {
        var pollId = Guid.NewGuid();
        var poll = new Poll
        {
            Id = pollId,
            CreatorId = Guid.NewGuid(),
            Title = "Test Poll",
            PollType = type,
            IsActive = isActive,
            CreatedAtUtc = DateTime.UtcNow
        };
        var options = new List<PollOption>
        {
            new() { Id = Guid.NewGuid(), PollId = pollId, Text = "Option A", DisplayOrder = 0 },
            new() { Id = Guid.NewGuid(), PollId = pollId, Text = "Option B", DisplayOrder = 1 }
        };
        return (poll, options);
    }

    // ── GetPoll ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPoll_WhenPollNotFound_Returns404()
    {
        _pollRepo.GetWithOptionsAsync(Arg.Any<Guid>()).Returns((ValueTuple<Poll, List<PollOption>>?)null);

        var result = await _controller.GetPoll(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetPoll_WhenFound_Returns200WithData()
    {
        var (poll, options) = MakePoll();
        _pollRepo.GetWithOptionsAsync(poll.Id).Returns((poll, options));

        var result = await _controller.GetPoll(poll.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<PollResponse>().Subject;
        response.Id.Should().Be(poll.Id);
        response.Title.Should().Be("Test Poll");
        response.Options.Should().HaveCount(2);
    }

    // ── Vote ─────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Vote_WhenPollNotFound_Returns404()
    {
        _pollRepo.GetWithOptionsAsync(Arg.Any<Guid>()).Returns((ValueTuple<Poll, List<PollOption>>?)null);

        var result = await _controller.Vote(Guid.NewGuid(), new VoteRequest { OptionIds = [Guid.NewGuid()] });

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task Vote_WhenPollIsInactive_Returns400()
    {
        var (poll, options) = MakePoll(isActive: false);
        _pollRepo.GetWithOptionsAsync(poll.Id).Returns((poll, options));

        var result = await _controller.Vote(poll.Id, new VoteRequest { OptionIds = [options[0].Id] });

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Vote_WhenAlreadyVoted_Returns409()
    {
        var (poll, options) = MakePoll();
        _pollRepo.GetWithOptionsAsync(poll.Id).Returns((poll, options));
        _voteRepo.HasVotedAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(true);

        var result = await _controller.Vote(poll.Id, new VoteRequest { OptionIds = [options[0].Id] });

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(409);
    }

    [Fact]
    public async Task Vote_SingleChoice_WithExactlyOneOption_Returns204()
    {
        var (poll, options) = MakePoll(PollType.SingleChoice);
        _pollRepo.GetWithOptionsAsync(poll.Id).Returns((poll, options));
        _voteRepo.HasVotedAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(false);
        _voteRepo.GetResultsAsync(poll.Id).Returns(new List<PollOptionResult>
        {
            new() { PollOptionId = options[0].Id, Text = "Option A", VoteCount = 1 },
            new() { PollOptionId = options[1].Id, Text = "Option B", VoteCount = 0 }
        });

        var result = await _controller.Vote(poll.Id, new VoteRequest { OptionIds = [options[0].Id] });

        result.Should().BeOfType<NoContentResult>();
        await _voteRepo.Received(1).CreateVoteAsync(Arg.Any<Vote>(), Arg.Any<List<VoteChoice>>());
    }

    [Fact]
    public async Task Vote_SingleChoice_WithTwoOptions_Returns400()
    {
        var (poll, options) = MakePoll(PollType.SingleChoice);
        _pollRepo.GetWithOptionsAsync(poll.Id).Returns((poll, options));
        _voteRepo.HasVotedAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(false);

        var result = await _controller.Vote(poll.Id, new VoteRequest
        {
            OptionIds = [options[0].Id, options[1].Id]
        });

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Vote_SingleChoice_WithZeroOptions_Returns400()
    {
        var (poll, options) = MakePoll(PollType.SingleChoice);
        _pollRepo.GetWithOptionsAsync(poll.Id).Returns((poll, options));
        _voteRepo.HasVotedAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(false);

        var result = await _controller.Vote(poll.Id, new VoteRequest { OptionIds = [] });

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Vote_MultipleChoice_WithZeroOptions_Returns400()
    {
        var (poll, options) = MakePoll(PollType.MultipleChoice);
        _pollRepo.GetWithOptionsAsync(poll.Id).Returns((poll, options));
        _voteRepo.HasVotedAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(false);

        var result = await _controller.Vote(poll.Id, new VoteRequest { OptionIds = [] });

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }

    [Fact]
    public async Task Vote_MultipleChoice_WithMultipleOptions_Returns204()
    {
        var (poll, options) = MakePoll(PollType.MultipleChoice);
        _pollRepo.GetWithOptionsAsync(poll.Id).Returns((poll, options));
        _voteRepo.HasVotedAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(false);
        _voteRepo.GetResultsAsync(poll.Id).Returns(new List<PollOptionResult>
        {
            new() { PollOptionId = options[0].Id, Text = "Option A", VoteCount = 1 },
            new() { PollOptionId = options[1].Id, Text = "Option B", VoteCount = 1 }
        });

        var result = await _controller.Vote(poll.Id, new VoteRequest
        {
            OptionIds = [options[0].Id, options[1].Id]
        });

        result.Should().BeOfType<NoContentResult>();
        await _voteRepo.Received(1).CreateVoteAsync(Arg.Any<Vote>(), Arg.Any<List<VoteChoice>>());
    }

    [Fact]
    public async Task Vote_WithInvalidOptionId_Returns400()
    {
        var (poll, options) = MakePoll(PollType.SingleChoice);
        _pollRepo.GetWithOptionsAsync(poll.Id).Returns((poll, options));
        _voteRepo.HasVotedAsync(Arg.Any<Guid>(), Arg.Any<string>()).Returns(false);

        // Submit a GUID that does not belong to this poll
        var result = await _controller.Vote(poll.Id, new VoteRequest { OptionIds = [Guid.NewGuid()] });

        var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
    }

    // ── CreatePoll ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task CreatePoll_WithValidRequest_Returns201()
    {
        var creator = new Creator { Id = Guid.NewGuid(), SecretToken = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow };
        _authService.GetCurrentCreatorAsync(Arg.Any<HttpContext>()).Returns((Creator?)null);
        _authService.CreateCreatorAsync(Arg.Any<HttpContext>(), Arg.Any<string?>()).Returns(creator);

        var request = new CreatePollRequest
        {
            Title = "Favourite language?",
            PollType = PollType.SingleChoice,
            Options = ["C#", "TypeScript"]
        };

        var result = await _controller.CreatePoll(request);

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.StatusCode.Should().Be(201);
        var response = created.Value.Should().BeOfType<CreatePollResponse>().Subject;
        response.SecretToken.Should().Be(creator.SecretToken);
        await _pollRepo.Received(1).CreateAsync(Arg.Any<Poll>(), Arg.Any<List<PollOption>>());
    }

    [Fact]
    public async Task CreatePoll_WhenCreatorCookieExists_ReusesExistingCreator()
    {
        var existingCreator = new Creator { Id = Guid.NewGuid(), SecretToken = Guid.NewGuid(), CreatedAtUtc = DateTime.UtcNow };
        _authService.GetCurrentCreatorAsync(Arg.Any<HttpContext>()).Returns(existingCreator);

        var request = new CreatePollRequest
        {
            Title = "Poll with existing creator",
            PollType = PollType.SingleChoice,
            Options = ["Yes", "No"]
        };

        var result = await _controller.CreatePoll(request);

        result.Should().BeOfType<CreatedAtActionResult>();
        // CreateCreatorAsync should NOT have been called since creator already exists
        await _authService.DidNotReceive().CreateCreatorAsync(Arg.Any<HttpContext>(), Arg.Any<string?>());
    }

    // ── GetResults ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetResults_WhenPollNotFound_Returns404()
    {
        _pollRepo.GetByIdAsync(Arg.Any<Guid>()).Returns((Poll?)null);

        var result = await _controller.GetResults(Guid.NewGuid());

        result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetResults_WhenPollExists_Returns200WithAggregatedResults()
    {
        var poll = new Poll { Id = Guid.NewGuid(), Title = "Test", PollType = PollType.SingleChoice, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
        _pollRepo.GetByIdAsync(poll.Id).Returns(poll);
        _voteRepo.GetResultsAsync(poll.Id).Returns(new List<PollOptionResult>
        {
            new() { PollOptionId = Guid.NewGuid(), Text = "A", VoteCount = 3 },
            new() { PollOptionId = Guid.NewGuid(), Text = "B", VoteCount = 1 }
        });

        var result = await _controller.GetResults(poll.Id);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var response = ok.Value.Should().BeOfType<PollResultsResponse>().Subject;
        response.TotalVotes.Should().Be(4);
        response.Options.Should().HaveCount(2);
        // Check percentages are calculated correctly
        response.Options.First(o => o.Text == "A").Percentage.Should().Be(75.0);
        response.Options.First(o => o.Text == "B").Percentage.Should().Be(25.0);
    }
}
