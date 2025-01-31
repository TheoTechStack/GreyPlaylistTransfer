using Microsoft.AspNetCore.Mvc;
using PlaylistTransfer.API.Agents;

namespace PlaylistTransfer.API.Controllers;

[Route("api/[controller]/[action]")]
[ApiController]

public class SpotifyController(SpotifyService spotifyService) : ControllerBase
{
    [HttpGet("")]
    public async Task<IActionResult> GetAuthUrl()
    {
        var url = await spotifyService.GenerateAuthUrl();
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
            // Exchange the authorization code for the access token
            var accessToken = await spotifyService.ExchangeCodeForTokenAsync(code);

            // Redirect the user to the playlists page with the access token as a query parameter
            return Redirect($"http://localhost:5200/playlists?accessToken={accessToken}");

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