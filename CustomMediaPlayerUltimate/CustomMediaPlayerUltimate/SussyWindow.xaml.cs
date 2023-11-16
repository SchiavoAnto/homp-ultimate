using System.IO;
using System.Windows;

namespace CustomMediaPlayerUltimate
{
    public partial class SussyWindow : Window
    {
        public SussyWindow()
        {
            InitializeComponent();
        }

        private void OnWindowLoaded(object sender, RoutedEventArgs e)
        {
            LoadAllSongs();
        }

        private void LoadAllSongs()
        {
            try
            {
                string[] allSongs = Directory.GetFiles(MainWindow.MUSIC_PATH, "*.mp3");
                foreach (string song in allSongs)
                {
                    string songName = song.Replace($"{MainWindow.MUSIC_PATH}\\", "").Replace(".mp3", "");
                    if (string.IsNullOrEmpty(songName) || string.IsNullOrWhiteSpace(songName)) continue;
                    ListStackPanel.Children.Add(new ListItem() { Content = songName });
                }
            }
            catch
            {
                MessageBox.Show("Unable to load songs.");
            }
        }
    }
}
