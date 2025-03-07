using Google.Apis.YouTube.v3.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using PlaylistTransfer.Shared;

namespace PlaylistTransfer.API.Agents
{
    public interface IYouTubeAgent
    {
        string GetAuthenticationUrl();
        Task<string> HandleCallbackAsync(string code);
        Task<string> CreatePlaylistAndAddVideoAsync(string playlistTitle, string playlistDescription, string videoTitle);
        Task<List<string>> CreatePlaylistsAndAddVideosAsync(List<PlaylistItems> playlists);
    }
}