using Microsoft.AspNetCore.Mvc;
using PlaylistTransfer.API.Agents;

namespace PlaylistTransfer.API.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]

public class SpotifyController(SpotifyService spotifyService) : ControllerBase
{
    [HttpGet("")]
    public IActionResult GetAuthUrl()
    {
        var url = spotifyService.GenerateAuthUrl();
        return Ok(new { Url = url });
    }
    
    [HttpGet("")]
    public async Task<IActionResult> GetAccessToken([FromQuery] string? code)
    {
        if (string.IsNullOrEmpty(code))
        {
            return BadRequest("Authorization code is missing.");
        }
        try
        {
            // Exchange the code for an access token
            var accessToken = await spotifyService.ExchangeCodeForTokenAsync(code);

            // Send the access token as a response or store it
            return Ok(new { AccessToken = accessToken });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { Error = "Failed to process authorization code", Details = ex.Message });
        }
    }
    
    [HttpGet("")]
    public async Task<IActionResult> GetPlaylists([FromHeader] string accessToken)
    {
        try
        {
            var playlists = await spotifyService.GetPlaylistsAsync(accessToken);
            return Ok(playlists);
        }
        catch (Exception ex)
        {
            return BadRequest(new { Error = ex.Message });
        }
    }
}