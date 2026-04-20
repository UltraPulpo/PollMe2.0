using System.Net;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using FluentAssertions;
using PollApp.Api.DTOs;
using PollApp.Api.Entities;

namespace PollApp.Api.Tests.Integration;

/// <summary>
/// End-to-end integration tests using the real ASP.NET Core pipeline + a
/// temporary SQLite database.  Each test gets a fresh <see cref="HttpClient"/>
/// whose cookie container is shared for the lifetime of a scenario (so the
/// creator_token cookie set by CreatePoll is automatically replayed on
/// subsequent requests within the same test).
/// </summary>
public class PollsIntegrationTests : IClassFixture<PollApiFactory>
{
    private readonly PollApiFactory _factory;

    // JSON options matching the server's JsonStringEnumConverter
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web)
    {
        Converters = { new System.Text.Json.Serialization.JsonStringEnumConverter() }
    };

    public PollsIntegrationTests(PollApiFactory factory)
    {
        _factory = factory;
    }

    // Each test gets its OWN HttpClient so cookies are isolated per test.
    private HttpClient NewClient() =>
        _factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            HandleCookies = true,   // auto-replay Set-Cookie headers
            AllowAutoRedirect = false
        });

    // ── Helpers ─────────────────────────────────────────────────────────────────

    private static StringContent Json(object obj) =>
        new(JsonSerializer.Serialize(obj, JsonOpts), Encoding.UTF8, "application/json");

    private static async Task<T> ReadJson<T>(HttpResponseMessage response)
    {
        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOpts)!;
    }

    private async Task<CreatePollResponse> CreatePollAsync(
        HttpClient client,
        string title = "Integration Test Poll",
        PollType pollType = PollType.SingleChoice)
    {
        var body = Json(new
        {
            title,
            pollType = pollType.ToString(),
            options = new[] { "Option A", "Option B" }
        });

        var response = await client.PostAsync("/api/polls", body);
        response.StatusCode.Should().Be(HttpStatusCode.Created,
            because: $"creating poll '{title}' should succeed");

        return await ReadJson<CreatePollResponse>(response);
    }

    private async Task<HttpResponseMessage> VoteAsync(
        HttpClient client, Guid pollId, Guid optionId) =>
        await client.PostAsync(
            $"/api/polls/{pollId}/vote",
            Json(new { optionIds = new[] { optionId } }));

    // ── Tests ────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullCycle_CreatePollVoteGetResults_VoteCountEqualsOne()
    {
        var client = NewClient();

        // 1. Create poll
        var created = await CreatePollAsync(client);
        created.PollId.Should().NotBe(Guid.Empty);

        // 2. GET the poll to retrieve option IDs
        var pollResponse = await client.GetAsync($"/api/polls/{created.PollId}");
        pollResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var poll = await ReadJson<PollResponse>(pollResponse);
        var firstOptionId = poll.Options.First().Id;

        // 3. Vote
        var voteResponse = await VoteAsync(client, created.PollId, firstOptionId);
        voteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // 4. Get results and assert vote count
        var resultsResponse = await client.GetAsync($"/api/polls/{created.PollId}/results");
        resultsResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var results = await ReadJson<PollResultsResponse>(resultsResponse);
        results.TotalVotes.Should().Be(1);
        results.Options.First(o => o.Id == firstOptionId).VoteCount.Should().Be(1);
    }

    [Fact]
    public async Task Vote_Twice_SecondVoteReturns409()
    {
        var client = NewClient();

        var created = await CreatePollAsync(client, "Double Vote Poll");

        var pollResponse = await client.GetAsync($"/api/polls/{created.PollId}");
        var poll = await ReadJson<PollResponse>(pollResponse);
        var optionId = poll.Options.First().Id;

        // First vote — should succeed
        var first = await VoteAsync(client, created.PollId, optionId);
        first.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Second vote — same voter_token cookie → 409
        var second = await VoteAsync(client, created.PollId, optionId);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreatorDashboard_ReturnsOnlyThatCreatorsPolls()
    {
        // Two separate clients = two separate creator identities
        var clientA = NewClient();
        var clientB = NewClient();

        var pollA = await CreatePollAsync(clientA, "Creator A Poll 1");
        await CreatePollAsync(clientA, "Creator A Poll 2");
        await CreatePollAsync(clientB, "Creator B Poll");

        // Dashboard is accessed via the secretToken route — no cookie required
        var dashA = await clientA.GetAsync($"/api/creator/{pollA.SecretToken}/polls");
        dashA.StatusCode.Should().Be(HttpStatusCode.OK);
        var summaries = await ReadJson<List<CreatorPollSummary>>(dashA);

        summaries.Should().HaveCount(2);
        summaries.Should().AllSatisfy(s => s.Title.Should().StartWith("Creator A Poll"));
    }

    [Fact]
    public async Task DeletePoll_SubsequentGetReturns404()
    {
        var client = NewClient();
        var created = await CreatePollAsync(client, "Poll To Delete");

        // DELETE requires the creator cookie (set automatically via HandleCookies = true)
        var deleteResponse = await client.DeleteAsync($"/api/polls/{created.PollId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        // Subsequent GET should return 404
        var getResponse = await client.GetAsync($"/api/polls/{created.PollId}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task TogglePollInactive_SubsequentVoteReturns400()
    {
        var client = NewClient();
        var created = await CreatePollAsync(client, "Poll To Toggle");

        // PATCH toggles IsActive — requires creator cookie
        var patchResponse = await client.PatchAsync($"/api/polls/{created.PollId}", null);
        patchResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var toggled = await ReadJson<PollResponse>(patchResponse);
        toggled.IsActive.Should().BeFalse();

        // GET the poll options via a fresh client (voter perspective)
        var voterClient = NewClient();
        var pollResponse = await voterClient.GetAsync($"/api/polls/{created.PollId}");
        var poll = await ReadJson<PollResponse>(pollResponse);
        var optionId = poll.Options.First().Id;

        // Try to vote on inactive poll
        var voteResponse = await VoteAsync(voterClient, created.PollId, optionId);
        voteResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetPoll_NonExistentPollId_Returns404()
    {
        var client = NewClient();
        var response = await client.GetAsync($"/api/polls/{Guid.NewGuid()}");
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeletePoll_WithoutCreatorAuth_Returns401()
    {
        // Creator creates the poll
        var creatorClient = NewClient();
        var created = await CreatePollAsync(creatorClient, "Auth Protected Poll");

        // A different client (no creator_token cookie) tries to delete
        var anonClient = NewClient();
        var deleteResponse = await anonClient.DeleteAsync($"/api/polls/{created.PollId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task DeletePoll_ByDifferentCreator_Returns403()
    {
        var ownerClient = NewClient();
        var created = await CreatePollAsync(ownerClient, "Someone Else's Poll");

        // A different creator creates their own poll (just to get a cookie)
        var otherClient = NewClient();
        await CreatePollAsync(otherClient, "Other Creator's Poll");

        // Other creator tries to delete owner's poll
        var deleteResponse = await otherClient.DeleteAsync($"/api/polls/{created.PollId}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }
}
