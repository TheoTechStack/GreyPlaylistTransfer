using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.WebUtilities;
using SpotifyAPI.Web;

namespace PlaylistTransfer.API.Agents;

public class SpotifyService
{
    //Azure KeyValut
    private const string ClientSecret = ""; 
    private const string ClientId = ""; 
    private const string RedirectUrl = "http://localhost:5095/api/Spotify/GetAccessToken";

    private readonly string[] _scopes =
    [
        "user-read-private",
        "user-read-email",
        "playlist-read-private",
        "playlist-read-collaborative"
    ];

    public string GenerateAuthUrl()
    {
        var queryParams = new Dictionary<string, string>(5)
        {
            { "response_type", "code" },
            { "client_id", ClientId },
            { "redirect_uri", RedirectUrl },
            { "scope", string.Join(" ", _scopes) }
        };

        return QueryHelpers.AddQueryString("https://accounts.spotify.com/authorize", queryParams!);
    }

    public async Task<string?> ExchangeCodeForTokenAsync(string code)
    {
        var authHeaderValue = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{ClientId}:{ClientSecret}"));
        using var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("Authorization", $"Basic {authHeaderValue}");

        var formData = new FormUrlEncodedContent(new[]
        {
            new KeyValuePair<string, string>("grant_type", "authorization_code"),
            new KeyValuePair<string, string>("code", code),
            new KeyValuePair<string, string>("redirect_uri", RedirectUrl)
        });

        var response = await httpClient.PostAsync("https://accounts.spotify.com/api/token", formData);

        if (!response.IsSuccessStatusCode)
            throw new Exception($"Error exchanging code: {response.ReasonPhrase}");

        var jsonResponse = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return jsonResponse.RootElement.GetProperty("access_token").GetString();
    }

    public async Task<List<object>> GetPlaylistsAsync(string accessToken)
    {
        var spotifyClient = new SpotifyClient(accessToken);
        var playlists = await spotifyClient.Playlists.CurrentUsers();

        var result = new List<object>();

        foreach (var playlist in playlists.Items!)
        {
            var playlistId = playlist.ExternalUrls?["spotify"].Split('/').Last();

            if (playlistId == null) continue;

            var detailedPlaylist = await spotifyClient.Playlists.Get(playlistId);
        
            // Initialize the list for this playlist's tracks
            var playlistTracks = new List<object>();

            foreach (var item in detailedPlaylist.Tracks!.Items!)
            {
                if (item.Track is not FullTrack track) continue;
                var artistNames = string.Join(", ", track.Artists.Select(artist => artist.Name));

                // Add the track to the playlistTracks list
                playlistTracks.Add(new
                {
                    TrackName = $"{track.Name} by {artistNames}",
                    TrackUrl = track.ExternalUrls!["spotify"]
                });
            }

            // If there are tracks, add the playlist with its tracks to the result
            if (playlistTracks.Count != 0)
            {
                result.Add(new
                {
                    PlaylistName = playlist.Name,
                    Tracks = playlistTracks
                });
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