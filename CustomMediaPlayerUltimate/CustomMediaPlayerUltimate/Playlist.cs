using System.Collections.Generic;

namespace CustomMediaPlayerUltimate
{
    internal class Playlist
    {

        public List<string> songs = new List<string>();

        public Playlist() { }

        public Playlist(string[] songs)
        {
            foreach (string song in songs)
            {
                string songName = song.Replace($"{MainWindow.MUSIC_PATH}\\", "").Replace(".mp3", "");
                if (string.IsNullOrEmpty(songName) || string.IsNullOrWhiteSpace(songName) || this.songs.Contains(songName)) continue;
                this.songs.Add(songName);
            }
        }

        public void AddSong(string songName)
        {
            if (string.IsNullOrEmpty(songName) || string.IsNullOrWhiteSpace(songName) || songs.Contains(songName)) return;
            songs.Add(songName);
        }

        public void RemoveSong(string songName)
        {
            if (string.IsNullOrEmpty(songName) || string.IsNullOrWhiteSpace(songName) || songs.Contains(songName)) return;
            songs.Remove(songName);
        }

    }
}
