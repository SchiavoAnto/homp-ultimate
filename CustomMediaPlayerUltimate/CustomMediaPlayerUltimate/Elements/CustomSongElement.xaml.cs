using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using CustomMediaPlayerUltimate.DataStructures;

namespace CustomMediaPlayerUltimate.Elements;

public partial class CustomSongElement : UserControl
{
    public class CustomSongElementInfo
    {
        public Song Song { get; private init; }
        public bool IsPlaying { get; set; } = false;
        public SongCollection Collection { get; private init; }

        public CustomSongElementInfo(Song song, SongCollection collection)
        {
            Song = song;
            Collection = collection;
        }
    }

    private CustomSongElementInfo? Info { get; set; }

    public bool Draggable
    {
        get { return (bool)GetValue(DraggableProperty); }
        set { SetValue(DraggableProperty, value); }
    }
    public static readonly DependencyProperty DraggableProperty =
        DependencyProperty.Register("Draggable", typeof(bool), typeof(CustomSongElement), new PropertyMetadata(false));

    public bool HoverEffects
    {
        get { return (bool)GetValue(HoverEffectsProperty); }
        set { SetValue(HoverEffectsProperty, value); }
    }
    public static readonly DependencyProperty HoverEffectsProperty =
        DependencyProperty.Register("HoverEffects", typeof(bool), typeof(CustomSongElement), new PropertyMetadata(true));

    public CustomSongElement()
    {
        InitializeComponent();
    }

    private void CustomSongElementLoaded(object sender, RoutedEventArgs e)
    {
        Info = (CustomSongElementInfo)Tag;
    }

    private void CustomSongElementDoubleClick(object sender, MouseButtonEventArgs e)
    {
        MainWindow.Instance.PlaySong(Info!.Song.FilePath, Info.Collection);
    }

    private void PlayMenuItemClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance.PlaySong(Info!.Song.FilePath, Info.Collection);
    }

    private void EditLyricsMenuItemClick(object sender, RoutedEventArgs e)
    {
        if (Info!.Song.HasErrored) return;
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
                if (MainWindow.Instance.currentSong is not null && MainWindow.Instance.currentSong.FilePath == Info.Song.FilePath)
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

    private void MakeFirstInQueueMenuItemClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance.MoveSongToTopOfQueue(Info);
    }

    private void DeleteMenuItemClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance.DeleteSong(Info!.Song);
    }

    private void PlayButtonClick(object sender, RoutedEventArgs e)
    {
        MainWindow.Instance.PlaySong(Info!.Song.FilePath, Info.Collection);
    }

    private void DragHandlePreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        DragDrop.DoDragDrop(this, this.DataContext, DragDropEffects.Move);
    }

    private void CustomSongElementDrop(object sender, DragEventArgs e)
    {
        MainWindow.Instance.QueueDropEvent(sender, e);
    }

    public void SetSongInfo(CustomSongElementInfo info)
    {
        Info = info;
        DataContext = Info;
    }
}
