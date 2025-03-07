using Microsoft.AspNetCore.Mvc;
using PlaylistTransfer.API.Agents;
namespace PlaylistTransfer.API.Controllers;

[Route("api/youtube")]
[ApiController]
public class YouTubeAuthController(IYouTubeAgent youTubeAgent) : ControllerBase
{
    [HttpGet("authenticate")]
    public IActionResult Authenticate()
    {
        var authUrl = youTubeAgent.GetAuthenticationUrl();
        return Ok(new { Url = authUrl });
    }
}

[Route("api/youtube")]
[ApiController]
public class YouTubeCallbackController(IYouTubeAgent youTubeAgent) : ControllerBase
{
    [HttpGet("callback")]
    public async Task<IActionResult> Callback([FromQuery] string code)
    {
        try
        {
            var redirectUrl = await youTubeAgent.HandleCallbackAsync(code);
            return Redirect(redirectUrl);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
            return StatusCode(500, "Authentication failed");
        }
    }
}


