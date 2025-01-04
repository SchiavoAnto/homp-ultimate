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
    public string Name { get; set; } = string.Empty;
    /// <summary>
    /// The type of the collection.
    /// </summary>
    public SongCollectionType Type { get; init; }
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

    public static readonly SongCollection EmptyPlaylist =
        new SongCollection(string.Empty, SongCollectionType.Playlist);
    public static readonly SongCollection EmptyAlbum =
        new SongCollection(string.Empty, SongCollectionType.Album);
    public static readonly SongCollection EmptyArtist =
        new SongCollection(string.Empty, SongCollectionType.Artist);

    private SongCollection() { }

    public SongCollection(string name, SongCollectionType type)
    {
        Name = name;
        Type = type;
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
            return Type == coll.Type
                && Name == coll.Name;
        }
        return false;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}

public enum SongCollectionType
{
    Playlist,
    Album,
    Artist
}
