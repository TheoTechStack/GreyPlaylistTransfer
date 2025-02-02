using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace PlaylistTransfer.API.Controllers;

[Route("api/youtube")]
[ApiController]
public class YouTubeAuthController : ControllerBase
{
    private static readonly string RedirectUri = "http://localhost:5095/api/youtube/callback";
    public static GoogleAuthorizationCodeFlow Flow;

    static YouTubeAuthController()
    {
        var clientSecrets = new ClientSecrets
        {
            ClientId = "279852309211-g83if6aar36jl7ggtj20ocsi15ibehbd.apps.googleusercontent.com",
            ClientSecret = "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"
        };

        Flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
        {
            ClientSecrets = clientSecrets,
            Scopes = new[] { YouTubeService.Scope.Youtube }
        });
    }

    [HttpGet("authenticate")]
    public IActionResult Authenticate()
    {
        string authUrl = Flow.CreateAuthorizationCodeRequest(RedirectUri).Build().AbsoluteUri;
        return Ok(new { Url = authUrl });
    }
}

[Route("api/youtube")]
[ApiController]
public class YouTubeCallbackController(IMemoryCache memoryCache) : ControllerBase
{
    private const string YouTubeServiceKey = "YouTubeService";

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code)
    {
        try
        {
            if (string.IsNullOrEmpty(code)) return BadRequest("Authorization code missing");

            var tokenResponse = await YouTubeAuthController.Flow.ExchangeCodeForTokenAsync(
                "user",
                code,
                "http://localhost:5095/api/youtube/callback",
                CancellationToken.None);

            var credential = new UserCredential(YouTubeAuthController.Flow, "user", tokenResponse);
            var youtubeService = new YouTubeService(new BaseClientService.Initializer
            {
                HttpClientInitializer = credential,
                ApplicationName = "YouTubeAPIApp"
            });

            // Store in-memory cache
            memoryCache.Set(YouTubeServiceKey, youtubeService, TimeSpan.FromMinutes(50));

            return Ok("Authentication successful");
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return StatusCode(500, "Authentication failed");
        }
    }
}


