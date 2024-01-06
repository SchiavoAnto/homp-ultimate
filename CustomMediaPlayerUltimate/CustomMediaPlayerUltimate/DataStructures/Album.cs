using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace CustomMediaPlayerUltimate.DataStructures;

public struct Album : SongCollection
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, Song> Songs { get; set; } = new();

    public static readonly Album Empty = new Album("");

    public Album(string name, Dictionary<string, Song> songs) : this(name)
    {
        Songs = songs;
    }

    public Album(string name)
    {
        Name = name;
    }

    public void AddSong(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrWhiteSpace(filePath)) return;
        Songs.Add(filePath, new Song(filePath));
    }

    public void AddSong(Song song)
    {
        Songs.Add(song.FilePath, song);
    }

    public bool RemoveSong(string filePath)
    {
        if (string.IsNullOrEmpty(filePath) || string.IsNullOrWhiteSpace(filePath)) return false;
        if (!Songs.ContainsKey(filePath)) return false;
        return Songs.Remove(filePath);
    }

    public bool RemoveSong(Song song)
    {
        return RemoveSong(song.FilePath);
    }

    public override bool Equals([NotNullWhen(true)] object? obj)
    {
        if (obj is null) return false;
        return Name == ((Album)obj).Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }
}
