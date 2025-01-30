namespace PlaylistTransfer.Shared;

//public record PlaylistItems(string? PlaylistName, List<object> Tracks, string? Image,  bool IsSelected = false );
public class PlaylistItems
{
    public string? PlaylistName { get; set; }
    public List<object> Tracks { get; set; }
    public string? Image { get; set; }
    public bool IsSelected { get; set; }

    public PlaylistItems(string? playlistName, List<object> tracks, string? image, bool isSelected = false)
    {
        PlaylistName = playlistName;
        Tracks = tracks;
        Image = image;
        IsSelected = isSelected;
    }
}