using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Services;
using Google.Apis.YouTube.v3;
using Google.Apis.YouTube.v3.Data;
using Microsoft.Extensions.Caching.Memory;
using System.Text.RegularExpressions;
using PlaylistTransfer.Shared;
using Playlist = Google.Apis.YouTube.v3.Data.Playlist;

namespace PlaylistTransfer.API.Agents
{
    public class YouTubeAgent(
        IMemoryCache memoryCache,
        YoutubeCredentialsProvider credentialsProvider)
        : IYouTubeAgent
    {
        private static readonly string RedirectUri = "http://localhost:5095/api/youtube/callback";
        private const string YouTubeServiceKey = "YouTubeService";
        private GoogleAuthorizationCodeFlow? _flow;
        private async Task InitializeFlowAsync()
        {
            if (_flow != null) return;

            var (clientId, clientSecret) = await credentialsProvider.GetYoutubeCredentialsAsync();

            var clientSecrets = new ClientSecrets
            {
                ClientId = clientId,
                ClientSecret = clientSecret
            };

            _flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = clientSecrets,
                Scopes = new[] { YouTubeService.Scope.Youtube }
            });
            
        }

        public async Task<string> GetAuthenticationUrl()
        {
            await InitializeFlowAsync(); 
            return _flow!.CreateAuthorizationCodeRequest(RedirectUri).Build().AbsoluteUri;
        }

        public async Task<string> HandleCallbackAsync(string code)
        {
            if (string.IsNullOrEmpty(code))
                throw new ArgumentException("Authorization code is required.");

            await InitializeFlowAsync();

            try
            {
                var tokenResponse = await _flow.ExchangeCodeForTokenAsync(
                    "user",
                    code,
                    RedirectUri,
                    CancellationToken.None);

                var credential = new UserCredential(_flow, "user", tokenResponse);
                var youtubeService = new YouTubeService(new BaseClientService.Initializer
                {
                    HttpClientInitializer = credential,
                    ApplicationName = "YouTubeAPIApp"
                });

                memoryCache.Set(YouTubeServiceKey, youtubeService, TimeSpan.FromMinutes(50));
                return "http://localhost:5200/youtube-music";
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to handle callback.", ex);
            }
        }

        public async Task<string> CreatePlaylistAndAddVideoAsync(string playlistTitle, string playlistDescription, string videoTitle)
        {
            if (!memoryCache.TryGetValue(YouTubeServiceKey, out YouTubeService youtubeService))
                throw new UnauthorizedAccessException("YouTube service not found. Please authenticate first.");

            try
            {
                var playlist = await CreateOrGetPlaylistAsync(youtubeService, playlistTitle, playlistDescription);
                if (playlist == null)
                    throw new Exception("Failed to create or retrieve playlist.");

                var videoId = await GetVideoIdByTitleAsync(youtubeService, videoTitle);
                if (string.IsNullOrEmpty(videoId))
                    throw new Exception("Video not found.");

                await AddVideoToPlaylistAsync(youtubeService, playlist.Id, videoId);
                return $"Video '{videoTitle}' added to playlist '{playlistTitle}'";
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to create playlist and add video.", ex);
            }
        }

        public async Task<List<string>> CreatePlaylistsAndAddVideosAsync(List<PlaylistItems> playlists)
        {
            if (!memoryCache.TryGetValue(YouTubeServiceKey, out YouTubeService youtubeService))
                throw new UnauthorizedAccessException("YouTube service not found. Please authenticate first.");

            var responseMessages = new List<string>();

            foreach (var playlist in playlists)
            {
                try
                {
                    var createdPlaylist = await CreateOrGetPlaylistAsync(youtubeService, playlist.PlaylistName, "");
                    if (createdPlaylist == null)
                    {
                        responseMessages.Add($"Failed to create or retrieve playlist '{playlist.PlaylistName}'");
                        continue;
                    }

                    var videoIds = await GetVideoIdsByTitlesAsync(youtubeService, playlist.Tracks);
                    if (videoIds.Count == 0)
                    {
                        responseMessages.Add($"No valid videos found for playlist '{playlist.PlaylistName}'");
                        continue;
                    }

                    foreach (var (_, videoId) in videoIds)
                    {
                        await AddVideoToPlaylistAsync(youtubeService, createdPlaylist.Id, videoId);
                    }

                    responseMessages.Add($"Added videos: {string.Join(", ", videoIds.Keys)} to playlist '{playlist.PlaylistName}'");
                }
                catch (Exception ex)
                {
                    responseMessages.Add($"Error processing playlist '{playlist.PlaylistName}': {ex.Message}");
                }
            }

            return responseMessages;
        }

        private async Task<Playlist> CreateOrGetPlaylistAsync(YouTubeService youtubeService, string title, string description)
        {
            try
            {
                var searchRequest = youtubeService.Playlists.List("snippet");
                searchRequest.Mine = true;
                var searchResponse = await searchRequest.ExecuteAsync();

                var existingPlaylist = searchResponse.Items.FirstOrDefault(p =>
                    p.Snippet.Title.Equals(title, StringComparison.OrdinalIgnoreCase));
                if (existingPlaylist != null) return existingPlaylist;

                var newPlaylist = new Playlist
                {
                    Snippet = new PlaylistSnippet { Title = title, Description = description },
                    Status = new PlaylistStatus { PrivacyStatus = "private" }
                };
                var request = youtubeService.Playlists.Insert(newPlaylist, "snippet,status");
                return await request.ExecuteAsync();
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to create or retrieve playlist.", ex);
            }
        }

        private async Task<string> GetVideoIdByTitleAsync(YouTubeService youtubeService, string title)
        {
            try
            {
                var searchRequest = youtubeService.Search.List("snippet");
                searchRequest.Q = title;
                searchRequest.Type = "video";
                searchRequest.MaxResults = 10;
                var searchResponse = await searchRequest.ExecuteAsync();

                var normalizedTitle = NormalizeText(title);

                var bestMatch = searchResponse.Items.MaxBy(v => GetSimilarity(normalizedTitle, NormalizeText(v.Snippet.Title)));

                return bestMatch?.Id.VideoId!;
            }
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to find video by title.", ex);
            }
        }

        private async Task<Dictionary<string, string>> GetVideoIdsByTitlesAsync(YouTubeService youtubeService, List<Track> tracks)
        {
            var videoIds = new Dictionary<string, string>();

            foreach (var track in tracks)
            {
                try
                {
                    var videoId = await GetVideoIdByTitleAsync(youtubeService, track.TrackName);
                    if (!string.IsNullOrEmpty(videoId))
                    {
                        videoIds[track.TrackName] = videoId;
                    }
                }
                catch (Exception ex)
                {
                    // Log the error and continue processing other tracks
                    Console.WriteLine($"Error finding video for track '{track.TrackName}': {ex.Message}");
                }
            }

            return videoIds;
        }

        private string NormalizeText(string text)
        {
            if (string.IsNullOrEmpty(text)) return string.Empty;
            text = text.ToLower();
            text = Regex.Replace(text, @"[^\w\s]", "");
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

            return dp[s1.Length, s2.Length];
        }

        private async Task AddVideoToPlaylistAsync(YouTubeService youtubeService, string playlistId, string videoId)
        {
            try
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
            catch (Exception ex)
            {
                throw new ApplicationException("Failed to add video to playlist.", ex);
            }
        }
    }
}