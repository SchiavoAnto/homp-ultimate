using System.IO;
using System.Windows;
using System.Windows.Controls;
using CustomMediaPlayerUltimate.DataStructures;

namespace CustomMediaPlayerUltimate.Elements;

public partial class CustomSongElement : UserControl
{
    public Song Song { get; set; }
    public SongCollection Collection { get; set; }

    private string _text = string.Empty;
    public string Text
    {
        get { return _text; }
        set
        {
            _text = value;
            ItemTextLabel.Content = value;
        }
    }

    private string _duration = string.Empty;
    public string Duration
    {
        get { return _duration; }
        set
        {
            _duration = value;
            ItemDurationLabel.Content = value;
        }
    }

    private bool _hasErrored = false;
    public bool HasErrored
    {
        get { return _hasErrored; }
        set
        {
            _hasErrored = value;
            PlayButton.Visibility = value ? Visibility.Hidden : Visibility.Visible;
        }
    }

    public CustomSongElement()
    {
        InitializeComponent();
        Collection = null!;
    }

    private void EditLyricsMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (HasErrored) return;
        BigInputBox bib = new BigInputBox("Insert song lyrics", $"Insert here the lyrics for '{Song.FileName}':");

        string lyricsFileName = $"{MainWindow.LYRICS_PATH}\\{Song.FileName}.mp3[Lyrics].txt";
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
                string lyricsPath = $"{MainWindow.LYRICS_PATH}\\{Song.FileName}.mp3[Lyrics].txt";
                string lyrics = bib.InputTextBox.Text;
                using (StreamWriter sw = new StreamWriter(lyricsPath))
                {
                    sw.Write(lyrics);
                    sw.Close();
                    sw.Dispose();
                }
                if (MainWindow.Instance.currentSong.HasValue && MainWindow.Instance.currentSong.Value.FilePath == Song.FilePath)
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
        MainWindow.Instance.SetPrioritySong(Song);
    }

    private void PlayButtonClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance.PlaySong(Song.FilePath, Collection);
    }
}
