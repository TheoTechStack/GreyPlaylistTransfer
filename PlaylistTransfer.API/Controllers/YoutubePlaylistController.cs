using Microsoft.AspNetCore.Mvc;
using PlaylistTransfer.API.Agents;
using PlaylistTransfer.Shared;

namespace PlaylistTransfer.API.Controllers;

[Route("api/youtube")]
[ApiController]
public class YoutubePlaylistController(IYouTubeAgent youTubeAgent) : ControllerBase
{
    [HttpPost("create-playlist-add-video1")]
    public async Task<IActionResult> CreatePlaylistAndAddVideo1([FromQuery] string playlistTitle, [FromQuery] string playlistDescription, [FromQuery] string videoTitle)
    {
        try
        {
            var result = await youTubeAgent.CreatePlaylistAndAddVideoAsync(playlistTitle, playlistDescription, videoTitle);
            return Ok(result);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }

    [HttpPost("create-playlists-and-add-videos")]
    public async Task<IActionResult> CreatePlaylistsAndAddVideos([FromBody] List<PlaylistItems> playlists)
    {
        try
        {
            var result = await youTubeAgent.CreatePlaylistsAndAddVideosAsync(playlists);
            return Ok(result);
        }
        catch (Exception e)
        {
            return BadRequest(e.Message);
        }
    }
}
