namespace PollApp.Api.Helpers;

public static class VoterTokenHelper
{
    /// <summary>
    /// Gets the voter token from the request cookie, or creates a new one
    /// and sets it on the response cookie. Returns the token string.
    /// </summary>
    public static string GetOrCreateVoterToken(HttpContext context)
    {
        if (context.Request.Cookies.TryGetValue("voter_token", out var existing))
            return existing;

        var token = Guid.NewGuid().ToString();
        context.Response.Cookies.Append("voter_token", token, new CookieOptions
        {
            HttpOnly = true,
            SameSite = SameSiteMode.Strict,
            Secure = false,
            MaxAge = TimeSpan.FromDays(365)
        });
        return token;
    }
}
