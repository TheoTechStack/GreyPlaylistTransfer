using System.Text.RegularExpressions;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Util;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using PlaylistTransfer.Shared;
using Playlist = Google.Apis.YouTube.v3.Data.Playlist;

namespace PlaylistTransfer.API.Controllers;

[Route("api/youtube")]
[ApiController]
public class YoutubePlaylistController(IMemoryCache memoryCache) : ControllerBase
{
    private readonly IMemoryCache _memoryCache = memoryCache;
    private const string YouTubeServiceKey = "YouTubeService";

    [Obsolete]
    [HttpPost("create-playlist-add-video1")]
    public async Task<IActionResult> CreatePlaylistAndAddVideo1([FromQuery] string playlistTitle, [FromQuery] string playlistDescription, [FromQuery] string videoTitle)
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
        await AddVideoToPlaylistAsync(youtubeService, playlist.Id, videoId);
        return Ok($"Video '{videoTitle}' added to playlist '{playlistTitle}'");
    }
    
    [HttpPost("create-playlists-and-add-videos")]
    public async Task<IActionResult> CreatePlaylistsAndAddVideos([FromBody] List<PlaylistItems> playlists)
    {
        if (!_memoryCache.TryGetValue(YouTubeServiceKey, out YouTubeService youtubeService))
        {
            return Unauthorized("YouTube service not found. Please authenticate first.");
        }

        var responseMessages = new List<string>();

        foreach (var playlist in playlists)
        {
            // Create or get existing playlist
            var createdPlaylist = await CreateOrGetPlaylist(youtubeService, playlist.PlaylistName, "");
            if (createdPlaylist == null)
            {
                responseMessages.Add($"Failed to create or retrieve playlist '{playlist.PlaylistName}'");
                continue;
            }

            // Get video IDs for all tracks in the playlist
            var videoIds = await GetVideoIdsByTitles(youtubeService, playlist.Tracks);
            if (videoIds.Count == 0)
            {
                responseMessages.Add($"No valid videos found for playlist '{playlist.PlaylistName}'");
                continue;
            }

            // Add valid videos to the playlist
            foreach (var (_, videoId) in videoIds)
            {
                await AddVideoToPlaylistAsync(youtubeService, createdPlaylist.Id, videoId);
            }

            responseMessages.Add($"Added videos: {string.Join(", ", videoIds.Keys)} to playlist '{playlist.PlaylistName}'");
        }

        return Ok(responseMessages);
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
    private async Task<Dictionary<string, string>> GetVideoIdsByTitles(YouTubeService youtubeService, List<Track> tracks)
    {
        var videoIds = new Dictionary<string, string>();

        foreach (var track in tracks)
        {
            var videoId = await GetVideoIdByTitle(youtubeService, track.TrackName);
            if (!string.IsNullOrEmpty(videoId))
            {
                videoIds[track.TrackName] = videoId;
            }
        }

        return videoIds;
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

    [Obsolete]
    private async Task<string> GetVideoIdByTitle2(YouTubeService youtubeService, string title)
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

    private async Task<string> GetVideoIdByTitle(YouTubeService youtubeService, string title)
    {
        var searchRequest = youtubeService.Search.List("snippet");
        searchRequest.Q = title;
        searchRequest.Type = "video";
        searchRequest.MaxResults = 10; // Increase results for better accuracy
        var searchResponse = await searchRequest.ExecuteAsync();

        var normalizedTitle = NormalizeText(title);

        var bestMatch = searchResponse.Items.MaxBy(v => GetSimilarity(normalizedTitle, NormalizeText(v.Snippet.Title)));

        return bestMatch?.Id.VideoId!;
    }

    private string NormalizeText(string text)
    {
        if (string.IsNullOrEmpty(text)) return string.Empty;
        text = text.ToLower();
        text = Regex.Replace(text, @"[^\w\s]", ""); // Remove special characters
        return text.Trim();
    }

    private int GetSimilarity(string s1, string s2)
    {
        if (string.IsNullOrEmpty(s1) || string.IsNullOrEmpty(s2)) return 0;

        int[,] dp = new int[s1.Length + 1, s2.Length + 1];

        for (int i = 0; i <= s1.Length; i++)
        {
            for (int j = 0; j <= s2.Length; j++)
            {
                if (i == 0 || j == 0)
                    dp[i, j] = i + j;
                else if (s1[i - 1] == s2[j - 1])
                    dp[i, j] = dp[i - 1, j - 1];
                else
                    dp[i, j] = 1 + Math.Min(dp[i - 1, j - 1], Math.Min(dp[i - 1, j], dp[i, j - 1]));
            }
        }

        return dp[s1.Length, s2.Length]; // Levenshtein Distance (Lower is better)
    }
        private async Task AddVideoToPlaylistAsync(YouTubeService youtubeService, string playlistId, string videoId)
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
