public class PlaylistDto
{
    public string PlaylistId { get; set; } // Unique identifier for the playlist
    public string PlaylistName { get; set; } // Name of the playlist
    public List<SongDto> Songs { get; set; } = new List<SongDto>(); // List of songs

    public bool IsSelected { get; set; } // To track selection for Apple Music
    public bool ShowDetails { get; set; } // To toggle showing songs in the playlist
}
public class SongDto
{
    public string SongName { get; set; } // Name of the song
    public string ArtistName { get; set; } // Name of the artist
}