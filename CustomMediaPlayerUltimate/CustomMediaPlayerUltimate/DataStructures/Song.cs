using System;
using System.Windows.Media.Imaging;

namespace CustomMediaPlayerUltimate.DataStructures;

public class Song
{
    public bool HasErrored { get; set; } = false;
    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Artist { get; set; } = string.Empty;
    public string? Album { get; set; } = null;
    public string Year { get; set; } = string.Empty;
    public TimeSpan Duration { get; set; } = TimeSpan.Zero;
    public string DurationString => Duration.ToString(MainWindow.TIME_FORMAT);
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
            (Album ?? MainWindow.UNKNOWN_ALBUM).ToLower().Contains(query) ||
            Year.Contains(query) ||
            Duration.ToString(MainWindow.TIME_FORMAT).Contains(query);
    }
}