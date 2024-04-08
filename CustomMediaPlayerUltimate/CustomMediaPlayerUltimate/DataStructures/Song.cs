namespace CustomMediaPlayerUltimate.DataStructures;

public struct Song
{
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public Album Album { get; set; } = Album.Empty;
    public string Year { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;

    public Song(string path, string title, string artist, Album album, string year, string duration) : this(path, title, artist, album, year)
    {
        Duration = duration;
    }

    public Song(string path, string title, string artist, Album album, string year) : this(path, title, artist, album)
    {
        Year = year;
    }

    public Song(string path, string title, string artist, Album album) : this(path, title, artist)
    {
        Album = album;
    }

    public Song(string path, string title, string artist) : this(path, title)
    {
        Artist = artist;
    }

    public Song(string path, string title) : this(path)
    {
        Title = title;
    }

    public Song(string path)
    {
        FilePath = path;
    }

    public bool IsCorrelated(string query)
    {
        query = query.ToLower();
        return FileName.ToLower().Contains(query) ||
            Title.ToLower().Contains(query) ||
            Artist.ToLower().Contains(query) ||
            Album.Name.ToLower().Contains(query) ||
            Year.Contains(query);
    }
}