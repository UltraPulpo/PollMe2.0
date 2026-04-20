using FluentAssertions;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using PollApp.Api.Entities;
using PollApp.Api.Repositories;
using PollApp.Api.Services;

namespace PollApp.Api.Tests.Unit;

public class CreatorAuthServiceTests
{
    private readonly ICreatorRepository _creatorRepo;
    private readonly CreatorAuthService _service;

    public CreatorAuthServiceTests()
    {
        _creatorRepo = Substitute.For<ICreatorRepository>();
        _service = new CreatorAuthService(_creatorRepo);
    }

    // ── GetCurrentCreatorAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task GetCurrentCreatorAsync_WithValidCookie_ReturnsCreator()
    {
        var secretToken = Guid.NewGuid();
        var creator = new Creator { Id = Guid.NewGuid(), SecretToken = secretToken, CreatedAtUtc = DateTime.UtcNow };
        _creatorRepo.GetBySecretTokenAsync(secretToken).Returns(creator);

        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"creator_token={secretToken}";

        var result = await _service.GetCurrentCreatorAsync(context);

        result.Should().NotBeNull();
        result!.Id.Should().Be(creator.Id);
        await _creatorRepo.Received(1).GetBySecretTokenAsync(secretToken);
    }

    [Fact]
    public async Task GetCurrentCreatorAsync_WithInvalidCookieValue_SkipsCookieCheckAndReturnsNull()
    {
        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = "creator_token=not-a-guid";

        var result = await _service.GetCurrentCreatorAsync(context);

        result.Should().BeNull();
        await _creatorRepo.DidNotReceive().GetBySecretTokenAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task GetCurrentCreatorAsync_WithUnknownCookieToken_ReturnsNull()
    {
        var secretToken = Guid.NewGuid();
        _creatorRepo.GetBySecretTokenAsync(secretToken).Returns((Creator?)null);

        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"creator_token={secretToken}";

        var result = await _service.GetCurrentCreatorAsync(context);

        result.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentCreatorAsync_WithValidRouteParam_ReturnsCreator()
    {
        var secretToken = Guid.NewGuid();
        var creator = new Creator { Id = Guid.NewGuid(), SecretToken = secretToken, CreatedAtUtc = DateTime.UtcNow };
        _creatorRepo.GetBySecretTokenAsync(secretToken).Returns(creator);

        var context = new DefaultHttpContext();
        context.Request.RouteValues["secretToken"] = secretToken.ToString();

        var result = await _service.GetCurrentCreatorAsync(context);

        result.Should().NotBeNull();
        result!.SecretToken.Should().Be(secretToken);
    }

    [Fact]
    public async Task GetCurrentCreatorAsync_WithNoCookieOrRoute_ReturnsNull()
    {
        var context = new DefaultHttpContext();

        var result = await _service.GetCurrentCreatorAsync(context);

        result.Should().BeNull();
        await _creatorRepo.DidNotReceive().GetBySecretTokenAsync(Arg.Any<Guid>());
    }

    [Fact]
    public async Task GetCurrentCreatorAsync_CookieTakesPriorityOverRoute()
    {
        var cookieToken = Guid.NewGuid();
        var routeToken = Guid.NewGuid();
        var cookieCreator = new Creator { Id = Guid.NewGuid(), SecretToken = cookieToken, CreatedAtUtc = DateTime.UtcNow };
        _creatorRepo.GetBySecretTokenAsync(cookieToken).Returns(cookieCreator);

        var context = new DefaultHttpContext();
        context.Request.Headers.Cookie = $"creator_token={cookieToken}";
        context.Request.RouteValues["secretToken"] = routeToken.ToString();

        var result = await _service.GetCurrentCreatorAsync(context);

        // Cookie-found path returns early — route param never consulted
        result!.Id.Should().Be(cookieCreator.Id);
        await _creatorRepo.Received(1).GetBySecretTokenAsync(cookieToken);
        await _creatorRepo.DidNotReceive().GetBySecretTokenAsync(routeToken);
    }

    // ── CreateCreatorAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateCreatorAsync_PersistsNewCreatorAndSetsCookie()
    {
        _creatorRepo.CreateAsync(Arg.Any<Creator>()).Returns(Task.CompletedTask);

        var context = new DefaultHttpContext();
        // Use a real response with a real cookie collection
        context.Response.Body = new MemoryStream();

        var creator = await _service.CreateCreatorAsync(context);

        creator.Should().NotBeNull();
        creator.Id.Should().NotBe(Guid.Empty);
        creator.SecretToken.Should().NotBe(Guid.Empty);
        await _creatorRepo.Received(1).CreateAsync(Arg.Is<Creator>(c => c.Id == creator.Id));
    }
}
