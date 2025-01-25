namespace PlaylistTransfer.Shared;

public class Playlist
{
    public bool? Collaborative { get; set; }

    public string? Description { get; set; }

    public Dictionary<string, string>? ExternalUrls { get; set; }

    public string? Href { get; set; }

    public string? Id { get; set; }

    public string? Name { get; set; }
    

    public bool? Public { get; set; }

    public string? SnapshotId { get; set; }

    public string? Type { get; set; }

    public string? Uri { get; set; }
}