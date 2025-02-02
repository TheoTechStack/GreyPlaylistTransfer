using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;

namespace PlaylistTransfer.API.Controllers;

[Route("api/youtube")]
[ApiController]
public class YoutubePlaylistController(IMemoryCache memoryCache) : ControllerBase
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private const string YouTubeServiceKey = "YouTubeService";

    [HttpPost("create-playlist-add-video")]
    public async Task<IActionResult> CreatePlaylistAndAddVideo([FromQuery] string playlistTitle, [FromQuery] string playlistDescription, [FromQuery] string videoTitle)
    {
        if (!_memoryCache.TryGetValue(YouTubeServiceKey, out YouTubeService youtubeService))
        {
            return Unauthorized("YouTube service not found. Please authenticate first.");
        }

        // Create or get existing playlist
        var playlist = await CreateOrGetPlaylist(youtubeService, playlistTitle, playlistDescription);
        if (playlist == null) return BadRequest("Failed to create or retrieve playlist");

        // Get video ID by title
        var videoId = await GetVideoIdByTitle(youtubeService, videoTitle);
        if (string.IsNullOrEmpty(videoId)) return NotFound("Video not found");

        // Add video to playlist
        await AddVideoToPlaylist(youtubeService, playlist.Id, videoId);
        return Ok($"Video '{videoTitle}' added to playlist '{playlistTitle}'");
    }

    private async Task<UserCredential> GetUserCredentialAsync()
    {
        var tokenResponse = await YouTubeAuthController.Flow.LoadTokenAsync("user", CancellationToken.None);
    
        if (tokenResponse == null)
        {
            Console.WriteLine("No token found. User needs to authenticate.");
            return null;
        }

        var credential = new UserCredential(YouTubeAuthController.Flow, "user", tokenResponse);

        if (tokenResponse.IsExpired(SystemClock.Default))
        {
            Console.WriteLine("Token expired, refreshing...");
            if (await credential.RefreshTokenAsync(CancellationToken.None))
            {
                Console.WriteLine("Token refreshed successfully.");
            }
            else
            {
                Console.WriteLine("Failed to refresh token. User might need to reauthenticate.");
                return null;
            }
        }

        return credential;
    }

    private async Task<Playlist> CreateOrGetPlaylist(YouTubeService youtubeService, string title, string description)
    {
        Playlist? results = null;
        try
        {
            var searchRequest = youtubeService.Playlists.List("snippet");
            searchRequest.Mine = true;
            var searchResponse = await searchRequest.ExecuteAsync();

            var existingPlaylist = searchResponse.Items.FirstOrDefault(p =>
                p.Snippet.Title.Equals(title, System.StringComparison.OrdinalIgnoreCase));
            if (existingPlaylist != null) return existingPlaylist;

            var newPlaylist = new Playlist
            {
                Snippet = new PlaylistSnippet { Title = title, Description = description },
                Status = new PlaylistStatus { PrivacyStatus = "private" }
            };
            var request = youtubeService.Playlists.Insert(newPlaylist, "snippet,status");
            results = await request.ExecuteAsync();
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
        
        return results!;
    }

    private async Task<string> GetVideoIdByTitle(YouTubeService youtubeService, string title)
        {
            var searchRequest = youtubeService.Search.List("snippet");
            searchRequest.Q = title;
            searchRequest.Type = "video";
            searchRequest.MaxResults = 5;
            var searchResponse = await searchRequest.ExecuteAsync();
            
            var searchWords = title
                .ToLower()
                .Replace("-", " ") // Optional, to handle hyphen variations
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries); // Split by spaces and remove empty entries

// Find the best match based on whether all search words are present in the title
            var bestMatch = searchResponse.Items
                .FirstOrDefault(v => 
                    searchWords.All(word => 
                        v.Snippet.Title
                            .ToLower()
                            .Contains(word))); // Ensure each word exists in the title

            return bestMatch?.Id.VideoId!;
        }

        private async Task AddVideoToPlaylist(YouTubeService youtubeService, string playlistId, string videoId)
        {
            var searchRequest = youtubeService.PlaylistItems.List("snippet");
            searchRequest.PlaylistId = playlistId;
            var searchResponse = await searchRequest.ExecuteAsync();

            if (searchResponse.Items.Any(item => item.Snippet.ResourceId.VideoId == videoId))
            {
                Console.WriteLine($"Video {videoId} is already in the playlist {playlistId}");
                return; 
            }
            
            var playlistItem = new PlaylistItem
            {
                Snippet = new PlaylistItemSnippet
                {
                    PlaylistId = playlistId,
                    ResourceId = new ResourceId { Kind = "youtube#video", VideoId = videoId }
                }
            };
            var request = youtubeService.PlaylistItems.Insert(playlistItem, "snippet");
            await request.ExecuteAsync();
        }
    }
