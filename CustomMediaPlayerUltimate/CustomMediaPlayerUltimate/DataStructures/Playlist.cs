using System.Collections.Generic;

namespace CustomMediaPlayerUltimate.DataStructures;

public struct Playlist : SongCollection
{
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, Song> Songs { get; set; } = new();

    public static readonly Playlist Empty = new Playlist("");

    public Playlist() { }

    public Playlist(string name, string[] songPaths) : this(name)
    {
        foreach (string songPath in songPaths)
        {
            AddSong(songPath);
        }
    }

    public Playlist(string name)
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
}