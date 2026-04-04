using PollApp.Api.Entities;

namespace PollApp.Api.Services;

public interface ICreatorAuthService
{
    /// <summary>
    /// Resolves the current creator from the HTTP context.
    /// Checks cookie first, then route parameter — returns null if neither present.
    /// </summary>
    Task<Creator?> GetCurrentCreatorAsync(HttpContext context);

    /// <summary>
    /// Creates a new creator, persists it, and sets the cookie on the response.
    /// </summary>
    Task<Creator> CreateCreatorAsync(HttpContext context, string? displayName = null);
}
