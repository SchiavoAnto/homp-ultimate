using System.Windows.Media.Imaging;

namespace CustomMediaPlayerUltimate.DataStructures;

public struct Song
{
    public bool HasErrored { get; set; } = false;
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public Album Album { get; set; } = Album.Empty;
    public string Year { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty;
    public BitmapImage? Cover { get; set; } = null;

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
            Year.Contains(query) ||
            Duration.Contains(query);
    }
}