using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Media.Imaging;

namespace CustomMediaPlayerUltimate.DataStructures;

public class SongCollection
{
    /// <summary>
    /// The name of the collection.
    /// </summary>
    public string Name { get; private set; } = string.Empty;
    /// <summary>
    /// The songs in this collection.
    /// </summary>
    public Dictionary<string, Song> Songs { get; private set; } = new();
    /// <summary>
    /// The cover image of this collection.
    /// </summary>
    public BitmapImage? Cover { get; set; } = null;

    public TimeSpan TotalTime =>
        TimeSpan.FromTicks(Songs.Values.Sum(song => song.Duration.Ticks));

    public static readonly SongCollection Empty = new SongCollection();

    private SongCollection() { }

    public SongCollection(string name)
    {
        Name = name;
    }

    public void AddSong(Song song)
    {
        if (song is null) return;
        if (song.FilePath is null) return;
        Songs.Add(song.FilePath, song);
    }

    public bool RemoveSong(Song song)
    {
        if (song is null) return false;
        if (song.FilePath is null) return false;
        return Songs.Remove(song.FilePath);
    }

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (obj is SongCollection coll)
        {
            return Name == coll.Name;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
