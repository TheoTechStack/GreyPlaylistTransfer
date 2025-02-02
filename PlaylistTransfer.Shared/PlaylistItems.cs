namespace PlaylistTransfer.Shared;

//public record PlaylistItems(string? PlaylistName, List<object> Tracks, string? Image,  bool IsSelected = false );
public record Track(string TrackName, string TrackUrl, string SongAlbum);
public class PlaylistItems(string? playlistName, List<Track> tracks, string? image, bool isSelected = false)
{
    public string? PlaylistName { get; } = playlistName;
    public List<Track> Tracks { get; } = tracks;
    public string? Image { get; } = image;
    public bool IsSelected { get; set; } = isSelected;
}