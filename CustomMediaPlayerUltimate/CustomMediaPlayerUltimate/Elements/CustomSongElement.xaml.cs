using System.IO;
using System.Windows;
using System.Windows.Controls;
using CustomMediaPlayerUltimate.DataStructures;

namespace CustomMediaPlayerUltimate.Elements;

public partial class CustomSongElement : UserControl
{
    public class CustomSongElementInfo
    {
        public Song Song { get; private init; }
        public SongCollection Collection { get; private init; }

        public CustomSongElementInfo(Song song, SongCollection collection)
        {
            Song = song;
            Collection = collection;
        }
    }

    private CustomSongElementInfo Info { get; set; }

    public CustomSongElement()
    {
        InitializeComponent();
    }

    private void CustomSongElementLoaded(object sender, RoutedEventArgs e)
    {
        Info = (CustomSongElementInfo)Tag;
    }

    private void EditLyricsMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (Info.Song.HasErrored) return;
        BigInputBox bib = new BigInputBox("Insert song lyrics", $"Insert here the lyrics for '{Info.Song.FileName}':");

        string lyricsFileName = $"{MainWindow.LYRICS_PATH}\\{Info.Song.FileName}.mp3[Lyrics].txt";
        if (File.Exists(lyricsFileName))
        {
            try
            {
                using (StreamReader sr = new StreamReader(lyricsFileName))
                {
                    string lyrics = sr.ReadToEnd();
                    bib.SetText(lyrics);
                }
            }
            catch
            {
                bib.SetText("Failed to load existing lyrics.");
            }
        }

        if (bib.ShowDialog() == true)
        {
            try
            {
                string lyricsPath = $"{MainWindow.LYRICS_PATH}\\{Info.Song.FileName}.mp3[Lyrics].txt";
                string lyrics = bib.InputTextBox.Text;
                using (StreamWriter sw = new StreamWriter(lyricsPath))
                {
                    sw.Write(lyrics);
                    sw.Close();
                    sw.Dispose();
                }
                if (MainWindow.Instance.currentSong.HasValue && MainWindow.Instance.currentSong.Value.FilePath == Info.Song.FilePath)
                {
                    MainWindow.Instance.LoadLyricsInView();
                }
            }
            catch
            {
                MessageBox.Show("An error occurred while trying to add lyrics to the song.");
            }
        }
    }

    private void PlayAsNextSongMenuItemClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance.SetPrioritySong(Info.Song);
    }

    private void PlayButtonClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance.PlaySong(Info.Song.FilePath, Info.Collection);
    }
}
