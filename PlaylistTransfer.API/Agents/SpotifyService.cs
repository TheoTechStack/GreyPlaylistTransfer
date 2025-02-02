using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Caching.Memory;
using PlaylistTransfer.Shared;
using SpotifyAPI.Web;

namespace PlaylistTransfer.API.Agents;

public class SpotifyService(SpotifyCredentialsProvider spotifyCredentialsProvider, IConfiguration configuration, IMemoryCache memoryCache)
{
    private const string CacheKey = "SpotifyAccessToken";
    private string _clientSecret = string.Empty; 
    private string _clientId = string.Empty; 
    private readonly string _redirectUrl = configuration["SpotifyRedirectUrl"]  ??
                                           throw new ArgumentNullException(nameof(configuration), 
                                               message: "SpotifyRedirectUrl is missing in configuration.");

    private readonly string[] _scopes =
    [
        "user-read-private",
        "user-read-email",
        "playlist-read-private",
        "playlist-read-collaborative"
    ];

    public async Task<string> GenerateAuthUrl()
    {
        (_clientId, _clientSecret) = await spotifyCredentialsProvider.GetSpotifyCredentialsAsync();
        var queryParams = new Dictionary<string, string>(5)
        {
            { "response_type", "code" },
            { "client_id", _clientId },
            { "redirect_uri", _redirectUrl },
            { "scope", string.Join(" ", _scopes) }
        };

        return QueryHelpers.AddQueryString("https://accounts.spotify.com/authorize", queryParams!);
    }

    public async Task<string?> ExchangeCodeForTokenAsync(string code)
    {
        if (memoryCache.TryGetValue(CacheKey, out string? cachedToken))
        {
            return cachedToken; // Return cached token if available
        }
        (_clientId, _clientSecret) = await spotifyCredentialsProvider.GetSpotifyCredentialsAsync();
        var authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_clientId}:{_clientSecret}"));
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authHeaderValue}");

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", _redirectUrl)
        });

        var response = await httpClient.PostAsync("https://accounts.spotify.com/api/token", formData);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Error exchanging code: {response.ReasonPhrase}");

        var jsonResponse = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var accessToken = jsonResponse.RootElement.GetProperty("access_token").GetString();

        // Store token in cache for 30 minutes
        if (!string.IsNullOrEmpty(accessToken))
        {
            memoryCache.Set(CacheKey, accessToken, TimeSpan.FromMinutes(30));
        }

        return accessToken;

    }

    public async Task<List<PlaylistItems>> GetPlaylistsAsync(string accessToken)
    {
        var spotifyClient = new SpotifyClient(accessToken);
        var playlists = await spotifyClient.Playlists.CurrentUsers();

        var result = new List<PlaylistItems>();

        foreach (var playlist in playlists.Items!)
        {
            var playlistId = playlist.ExternalUrls?["spotify"].Split('/').Last();

            if (playlistId == null) continue;

            var detailedPlaylist = await spotifyClient.Playlists.Get(playlistId);
            
            var playlistTracks = new List<Track>();

            foreach (var item in detailedPlaylist.Tracks!.Items!)
            {
                if (item.Track is not FullTrack track) continue;
                var artistNames = string.Join(", ", track.Artists.Select(artist => artist.Name));
                var songAlbum = track.Album.Images.Select(image => image.Url).FirstOrDefault();
                
                playlistTracks.Add(new Track(
                    $"{track.Name} - {artistNames}", 
                    track.ExternalUrls!["spotify"],
                    songAlbum!));
            }

            // If there are tracks, add the playlist with its tracks to the result
            if (playlistTracks.Count != 0)
            {
                result.Add(new PlaylistItems
                (
                     playlist.Name,
                     playlistTracks,
                    playlist.Images!.Select(item => item.Url).FirstOrDefault()
                ));
            }
        }

        return result;
    }


    [Obsolete]
    public async Task<List<object>> GetPlaylistsAsyncv3(string accessToken)
    {
        var spotifyClient = new SpotifyClient(accessToken);
        var playlists = await spotifyClient.Playlists.CurrentUsers();

        var result = new List<object>();

        foreach (var playlist in playlists.Items!)
        {
            var playlistId = playlist.ExternalUrls?["spotify"].Split('/').Last();

            if (playlistId == null) continue;
            // Fetch detailed playlist information including tracks
            var detailedPlaylist = await spotifyClient.Playlists.Get(playlistId);
            
            // Use LINQ to select the track details along with the playlist name
            var tracks = detailedPlaylist.Tracks!.Items?
                .Where(item => item.Track is FullTrack track) // Filter out items that aren't FullTrack
                .Select(item => item.Track as FullTrack) // Select the actual FullTrack
                .Where(track => track != null) // Ensure track isn't null
                .Select(track => new
                {
                    PlaylistName = playlist.Name,
                    TrackName = track.Name,
                    Artists = string.Join(", ", track.Artists.Select(artist => artist.Name)),
                    TrackUrl = track.ExternalUrls!["spotify"]
                }).ToList();

            if (tracks != null)
            {
                result.AddRange(tracks);
            }
        }

        return result;
    }

    
    [Obsolete]
    public async Task<List<object>> GetPlaylistsAsyncv1(string accessToken)
    {
        var spotifyClient = new SpotifyClient(accessToken);
        var playlists = await spotifyClient.Playlists.CurrentUsers();
        return playlists.Items!.Select(p => new
        {
            p.Name,
            p.Tracks!.Total,
            Url = p.ExternalUrls!["spotify"]
        }).ToList<object>();
    }
}