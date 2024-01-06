using System.Collections.Generic;

namespace CustomMediaPlayerUltimate.DataStructures;

public interface SongCollection
{
    public Dictionary<string, Song> Songs { get; set; }

    void AddSong(string filePath);

    void AddSong(Song song);

    bool RemoveSong(string filePath);

    bool RemoveSong(Song song);
}
