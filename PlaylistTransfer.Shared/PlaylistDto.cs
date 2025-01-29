public class PlaylistDto
{
    public string PlaylistId { get; set; } 
    public string PlaylistName { get; set; } 
    public List<SongDto> Songs { get; set; } = []; // List of songs
    public string ImageUrl { get; set; } 
    public bool IsSelected { get; set; } // To track selection for Apple Music
    public bool ShowDetails { get; set; } // To toggle showing songs in the playlist
}
public class SongDto
{
    public string SongName { get; set; } // Name of the song
    public string ArtistName { get; set; } // Name of the artist
}