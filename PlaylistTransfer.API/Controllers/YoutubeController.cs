using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using PlaylistTransfer.API.Agents;

namespace PlaylistTransfer.API.Controllers;

[Route("api/youtube")]
[ApiController]
public class YouTubeAuthController(YouTubeAgent youTubeAgent) : ControllerBase
{
    [HttpGet("authenticate")]
    public IActionResult Authenticate()
    {
        string authUrl = youTubeAgent.GetAuthenticationUrl();
        return Ok(new { Url = authUrl });
    }
}

[Route("api/youtube")]
[ApiController]
public class YouTubeCallbackController(YouTubeAgent youTubeAgent) : ControllerBase
{
    private readonly YouTubeAgent _youTubeAgent = youTubeAgent;

    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code)
    {
        try
        {
            var redirectUrl = await _youTubeAgent.HandleCallbackAsync(code);
            return Redirect(redirectUrl);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return StatusCode(500, "Authentication failed");
        }
    }
}


