using PollApp.Api.Entities;
using PollApp.Api.Repositories;

namespace PollApp.Api.Services;

public class CreatorAuthService : ICreatorAuthService
{
    private readonly ICreatorRepository _creatorRepository;

    public CreatorAuthService(ICreatorRepository creatorRepository)
    {
        _creatorRepository = creatorRepository;
    }

    public async Task<Creator?> GetCurrentCreatorAsync(HttpContext context)
    {
        // 1. Check cookie first
        if (context.Request.Cookies.TryGetValue("creator_token", out var cookieValue)
            && Guid.TryParse(cookieValue, out var cookieToken))
        {
            var creator = await _creatorRepository.GetBySecretTokenAsync(cookieToken);
            if (creator != null)
                return creator;
        }

        // 2. Check route parameter (magic link)
        if (context.Request.RouteValues.TryGetValue("secretToken", out var routeValue)
            && routeValue is string routeString
            && Guid.TryParse(routeString, out var routeToken))
        {
            var creator = await _creatorRepository.GetBySecretTokenAsync(routeToken);
            if (creator != null)
                return creator;
        }

        return null;
    }

    public async Task<Creator> CreateCreatorAsync(HttpContext context, string? displayName = null)
    {
        var creator = new Creator
        {
            Id = Guid.NewGuid(),
            SecretToken = Guid.NewGuid(),
            DisplayName = displayName,
            CreatedAtUtc = DateTime.UtcNow
        };

        await _creatorRepository.CreateAsync(creator);

        context.Response.Cookies.Append("creator_token", creator.SecretToken.ToString(), new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = false,
            MaxAge = TimeSpan.FromDays(365)
        });

        return creator;
    }
}
