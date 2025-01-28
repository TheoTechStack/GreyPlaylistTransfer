using PlaylistTransfer.Shared;

namespace PlaylistTransfer.UI.Data;

public class PlaylistState
{
    
    public static List<PlaylistDto> SelectedPlaylists { get; set; } = new List<PlaylistDto>();
}