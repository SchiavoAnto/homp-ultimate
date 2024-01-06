using System;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.Win32;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using CustomMediaPlayerUltimate.Elements;
using CustomMediaPlayerUltimate.DataStructures;

namespace CustomMediaPlayerUltimate;

public partial class MainWindow : Window
{
    public static string MUSIC_PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
    public static string PLAYLISTS_PATH = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)}\\HompPlaylists";
    public static string LYRICS_PATH = $"{Environment.GetFolderPath(Environment.SpecialFolder.MyMusic)}\\Lyrics";
    private const int VOLUME_STEP = 2;
    public static MainWindow Instance = null!;

    private MediaPlayer mediaPlayer = new MediaPlayer();
    private DispatcherTimer timer = new DispatcherTimer();
    private bool isPlaying = false;
    private bool IsPlaying
    {
        get { return isPlaying; }
        set
        {
            isPlaying = value;
            if (value)
            {
                SetPlayPauseImage(true);
            }
            else
            {
                SetPlayPauseImage(false);
            }
        }
    }
    private bool mediaAvailable = false;
    private bool isProgressSliderBeingDragged = false;
    private bool isVolumeSliderBeingDragged = false;
    private Song? currentSong = null;

    private SongCollection? currentCollection;
    private Playlist allSongsPlaylist;
    private Dictionary<string, Playlist> playlists = new Dictionary<string, Playlist>();
    private Dictionary<string, Album> albums = new Dictionary<string, Album>();
    private List<Tuple<string, SongCollection>> playedSongs = new List<Tuple<string, SongCollection>>();

    public MainWindow()
    {
        InitializeComponent();

        Instance = this;

        KeyboardHook.OnKeyPressed += HandleHotkey!;
        KeyboardHook.Start();
    }

    public void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        //Carichiamo tutte le canzoni
        LoadAllSongs();
        //Carichiamo tutte le playlist
        LoadAllPlaylists();
        //Carichiamo tutti gli album
        //LoadAllAlbums();
        //Carichiamo le impostazioni
        LoadSettings();

        mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
        mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;

        timer.Interval = TimeSpan.FromMilliseconds(100);
        timer.Tick += Timer_Tick!;
        timer.Start();
    }

    public void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Properties.Settings.Default.Save();
        KeyboardHook.Stop();
    }

    private void HandleHotkey(object sender, Key key)
    {
        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift))
        {
            if (key == Key.P)
            {
                TogglePlayPause();
            }
            else if (key == Key.Up)
            {
                IncreaseVolume();
            }
            else if (key == Key.Down)
            {
                DecreaseVolume();
            }
            else if (key == Key.N)
            {
                NextSongInPlaylist();
            }
            else if (key == Key.B)
            {
                PreviousSongInPlaylist();
            }
            else if (key == Key.R)
            {
                ToggleLoop();
            }
            else if (key == Key.S)
            {
                ToggleShuffle();
            }
        }
    }

    public void SwitchToAllSongsView(object sender, RoutedEventArgs e)
    {
        PlaylistsView.Visibility = Visibility.Collapsed;
        SearchResultsView.Visibility = Visibility.Collapsed;
        AllSongsView.Visibility = Visibility.Visible;
    }

    public void SwitchToPlaylistsView(object sender, RoutedEventArgs e)
    {
        AllSongsView.Visibility = Visibility.Collapsed;
        SearchResultsView.Visibility = Visibility.Collapsed;
        PlaylistsView.Visibility = Visibility.Visible;
    }

    public void SwitchToSearchResultsView(object sender, RoutedEventArgs e)
    {
        AllSongsView.Visibility = Visibility.Collapsed;
        PlaylistsView.Visibility = Visibility.Collapsed;
        SearchResultsView.Visibility = Visibility.Visible;
    }

    //public void AllSongsListViewDoubleClick(object sender, RoutedEventArgs e)
    //{
    //    DependencyObject src = VisualTreeHelper.GetParent((DependencyObject)e.OriginalSource);
    //    if (src is Control && src.GetType() != typeof(ListViewItem)) return;
    //    if (AllSongsListView.SelectedItem == null) return;
    //    PlaySong($"{AllSongsListView.SelectedItem}", allSongsPlaylist);
    //}

    public void PlaylistsSongsListViewDoubleClick(object sender, RoutedEventArgs e)
    {
        DependencyObject src = VisualTreeHelper.GetParent((DependencyObject)e.OriginalSource);
        if (src is Control && src.GetType() != typeof(ListViewItem)) return;
        if (PlaylistsSongsListView.SelectedItem == null) return;
        PlaySong($"{PlaylistsSongsListView.SelectedItem}", playlists[PlaylistsListView.SelectedItem.ToString()!]);
    }

    public void PlayPauseButtonClick(object sender, RoutedEventArgs e)
    {
        TogglePlayPause();
    }

    private void TogglePlayPause()
    {
        if (IsPlaying)
        {
            if (mediaPlayer.CanPause) mediaPlayer.Pause();
            IsPlaying = false;
        }
        else
        {
            mediaPlayer.Play();
            IsPlaying = true;
        }
    }

    public void ProgressSliderMouseMove(object sender, RoutedEventArgs e)
    {
        if (!isProgressSliderBeingDragged) return;
        mediaPlayer.Position = TimeSpan.FromSeconds(ProgressSlider.Value);
    }

    public void ProgressSliderMouseDown(object sender, RoutedEventArgs e)
    {
        isProgressSliderBeingDragged = true;
    }

    public void ProgressSliderMouseUp(object sender, RoutedEventArgs e)
    {
        isProgressSliderBeingDragged = false;
    }

    public void VolumeSliderMouseMove(object sender, RoutedEventArgs e)
    {
        if (!isVolumeSliderBeingDragged) return;
        mediaPlayer.Volume = VolumeSlider.Value / 100f;
        Properties.Settings.Default.PlayerVolume = VolumeSlider.Value;
        VolumeLabel.Content = $"Volume: {VolumeSlider.Value}%";
    }

    public void VolumeSliderMouseDown(object sender, RoutedEventArgs e)
    {
        isVolumeSliderBeingDragged = true;
    }

    public void VolumeSliderMouseUp(object sender, RoutedEventArgs e)
    {
        isVolumeSliderBeingDragged = false;
    }

    public void VolumeSliderMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (e.Delta > 0)
        {
            IncreaseVolume();
        }
        else
        {
            DecreaseVolume();
        }
    }

    public void LoopToggleButtonClick(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.PlayerLoop = (bool)LoopToggleButton.IsChecked!;
    }

    public void ShuffleToggleButtonClick(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.PlayerShuffle = (bool)ShuffleToggleButton.IsChecked!;
    }

    public void BackwardButtonClick(object sender, RoutedEventArgs e)
    {
        PreviousSongInPlaylist();
    }

    public void ForwardButtonClick(object sender, RoutedEventArgs e)
    {
        NextSongInPlaylist();
    }

    public void NewPlaylistButtonClick(object sender, RoutedEventArgs e)
    {
        InputBox ib = new InputBox("Insert playlist name", "Type the name you want to give to the playlist:");
        if (ib.ShowDialog() == true)
        {
            string name = ib.InputTextBox.Text;
            string path = $"{PLAYLISTS_PATH}\\{name}.homppl";
            try
            {
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.Write("");
                    sw.Close();
                    sw.Dispose();
                }
                playlists.Add(name, new Playlist());
                PlaylistsListView.Items.Add(name);
            }
            catch
            {
                MessageBox.Show("An error occurred while trying to create the new playlist.");
            }
        }
    }

    public void RenamePlaylistButtonClick(object sender, RoutedEventArgs e)
    {
        if (PlaylistsListView.SelectedItem == null) return;
        string playlistName = PlaylistsListView.SelectedItem.ToString()!;
        string oldPath = $"{PLAYLISTS_PATH}\\{playlistName}.homppl";
        InputBox ib = new InputBox("Insert playlist name", "Type the new name you want to give to the playlist:");
        if (ib.ShowDialog() == true)
        {
            string newName = ib.InputTextBox.Text;
            string newPath = $"{PLAYLISTS_PATH}\\{newName}.homppl";
            try
            {
                File.Move(oldPath, newPath);
                PlaylistsListView.Items.Remove(PlaylistsListView.SelectedItem);
                PlaylistsListView.Items.Add(newName);

                Playlist pl = playlists[playlistName];
                playlists.Remove(playlistName);
                playlists.Add(newName, pl);
            }
            catch
            {
                MessageBox.Show("An error occurred while trying to rename the playlist.");
            }
        }
    }

    public void DeletePlaylistButtonClick(object sender, RoutedEventArgs e)
    {
        if (PlaylistsListView.SelectedItem == null) return;
        string playlistName = PlaylistsListView.SelectedItem.ToString()!;
        string path = $"{PLAYLISTS_PATH}\\{playlistName}.homppl";
        if (MessageBox.Show($"Are you sure to delete the playlist \"{playlistName}\"?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            try
            {
                File.Delete(path);
                PlaylistsListView.Items.Remove(PlaylistsListView.SelectedItem);
                PlaylistsSongsListView.Items.Clear();
            }
            catch
            {
                MessageBox.Show("An error occurred while trying to delete the selected playlist.");
            }
        }
    }

    //TODO: Replace OpenFileDialog with a custom dialog from where the user can choose songs from the all songs list.
    public void AddSongsToPlaylistButtonClick(object sender, RoutedEventArgs e)
    {
        if (PlaylistsListView.SelectedItem == null) return;
        string playlistName = PlaylistsListView.SelectedItem.ToString()!;
        string path = $"{PLAYLISTS_PATH}\\{playlistName}.homppl";

        OpenFileDialog ofd = new OpenFileDialog();
        ofd.Multiselect = true;
        ofd.InitialDirectory = MUSIC_PATH;
        ofd.Title = $"Select the songs to add to the playlist \"{playlistName}\"...";
        ofd.Filter = "MP3 Files|*.mp3";
        if (ofd.ShowDialog() == true)
        {
            string[] selectedSongs = ofd.FileNames;
            try
            {
                using (StreamWriter sw = new StreamWriter(path, true))
                {
                    foreach (string song in selectedSongs)
                    {
                        string actualName = new FileInfo(song).Name.Replace(".mp3", "");
                        sw.Write($"{song}|");
                        playlists[playlistName].AddSong(actualName);
                        PlaylistsSongsListView.Items.Add(actualName);
                    }
                    sw.Close();
                    sw.Dispose();
                }
            }
            catch
            {
                MessageBox.Show("An error occurred while adding the selected songs to the playlist.");
            }
        }
    }

    public void RemoveSongsFromPlaylistButtonClick(object sender, RoutedEventArgs e)
    {
        if (PlaylistsListView.SelectedItem == null) return;
        string playlistName = PlaylistsListView.SelectedItem.ToString()!;
        string playlistPath = $"{PLAYLISTS_PATH}\\{playlistName}.homppl";

        if (PlaylistsSongsListView.SelectedItem == null) return;
        string songName = PlaylistsSongsListView.SelectedItem.ToString()!;
        string songPath = $"{MUSIC_PATH}\\{songName}.mp3";

        if (MessageBox.Show($"Are you sure to remove the song \"{songName}\" from the playlist?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            try
            {
                using (StreamReader sr = new StreamReader(playlistPath))
                {
                    string playlistContent = sr.ReadToEnd();
                    sr.Close();
                    sr.Dispose();
                    using (StreamWriter sw = new StreamWriter(playlistPath))
                    {
                        sw.Write(playlistContent.Replace($"{songPath}|", ""));
                        sw.Close();
                        sw.Dispose();
                    }
                }
                PlaylistsSongsListView.Items.Remove(songName);
                playlists[playlistName].RemoveSong(songName);
            }
            catch
            {
                MessageBox.Show("An error occurred while removing the song from the playlist.");
            }
        }
    }

    public void LoadLyricsInView()
    {
        if (currentSong is null) return;
        string lyricsFileName = $"{LYRICS_PATH}\\{currentSong.Value.FileName}.mp3[Lyrics].txt";
        if (!File.Exists(lyricsFileName))
        {
            SetSongLyricsRichTextBoxText("No lyrics for this song.");
            return;
        }

        try
        {
            using (StreamReader sr = new StreamReader(lyricsFileName))
            {
                string lyrics = sr.ReadToEnd();
                SetSongLyricsRichTextBoxText(lyrics);
            }
        }
        catch
        {
            SetSongLyricsRichTextBoxText("An error occurred while trying to load lyrics for this song.");
        }
    }

    public void LoadPlaylistSongsInView(object sender, RoutedEventArgs e)
    {
        DependencyObject src = VisualTreeHelper.GetParent((DependencyObject)e.OriginalSource);
        if (src is Control && src.GetType() != typeof(ListViewItem)) return;
        if (PlaylistsListView.SelectedItem == null) return;
        SongCollection? playlist = playlists[PlaylistsListView.SelectedItem.ToString()!];
        PlaylistsSongsListView.Items.Clear();
        foreach (Song song in playlist.Songs.Values)
        {
            PlaylistsSongsListView.Items.Add(song.FilePath);
        }
    }

    public void SearchInputTextBoxKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            IEnumerable<Song> results = new List<Song>();
            foreach (Song song in allSongsPlaylist.Songs.Values)
            {
                if (!song.Title.ToLower().Contains(SearchInputTextBox.Text.ToLower())) continue;
                results.Append(song);
            }
            SearchResultSongsListView.Items.Clear();
            foreach (Song song in results)
            {
                SearchResultSongsListView.Items.Add(song.FilePath);
            }
            SwitchToSearchResultsView(null!, null!);
        }
    }

    private void LoadAllSongs()
    {
        try
        {
            string[] songPaths = Directory.GetFiles(MUSIC_PATH, "*.mp3");
            allSongsPlaylist = new Playlist("__HOMP_ALL_SONGS_PLAYLIST__");
            Dictionary<string, string> props = new();
            foreach (string songPath in songPaths)
            {
                Song song = new Song(songPath);
                try
                {
                    song.FileName = songPath.Replace($"{MUSIC_PATH}\\", "").Replace(".mp3", "");
                    props?.Clear();
                    props = Utils.ReadID3v2Tags(songPath);
                    string? title = null;
                    string? artistName = null;
                    string? albumName = null;
                    string? year = null;
                    if (props is not null)
                    {
                        if (props.TryGetValue("TIT2", out title)) song.Title = title;
                        if (props.TryGetValue("TPE1", out artistName)) song.Artist = artistName;
                        if (props.TryGetValue("YEAR", out year)) song.Year = year;
                        if (props.TryGetValue("TALB", out albumName))
                        {
                            if (albums.ContainsKey(albumName))
                            {
                                albums[albumName].AddSong(song);
                                song.Album = albums[albumName];
                            }
                            else
                            {
                                Album album = new Album(albumName);
                                albums[albumName] = album;
                                song.Album = albums[albumName];
                            }
                        }
                    }
                    allSongsPlaylist.AddSong(song);
                    AllSongsListPanel.Children.Add(new CustomSongElement()
                    {
                        Text = title is null ? songPath : song.FileName,
                        Song = song,
                        Collection = allSongsPlaylist,
                        HasErrored = false
                    });
                }
                catch
                {
                    // No need to pass Song and Collection because an errored item
                    // does not show a playing button.
                    AllSongsListPanel.Children.Add(new CustomSongElement()
                    {
                        Text = song.FileName,
                        HasErrored = true
                    });
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show("Unable to load songs.\n\n" + ex.Message + "\n" + ex.StackTrace + ex.InnerException?.Message);
        }
    }

    private void LoadAllPlaylists()
    {
        if (!Directory.Exists(PLAYLISTS_PATH)) return;
        try
        {
            string[] allPlaylists = Directory.GetFiles(PLAYLISTS_PATH, "*.homppl");
            foreach (string p in allPlaylists)
            {
                Playlist playlist = new Playlist();
                string playlistName = p.Replace($"{PLAYLISTS_PATH}\\", "").Replace(".homppl", "");
                PlaylistsListView.Items.Add(playlistName);
                using (StreamReader sr = new StreamReader(p))
                {
                    string allSongs = sr.ReadToEnd();
                    if (!string.IsNullOrEmpty(allSongs) && !string.IsNullOrWhiteSpace(allSongs))
                    {
                        if (allSongs.Contains("|"))
                        {
                            string[] songs = allSongs.Split("|");
                            playlist = new Playlist(playlistName, songs);
                        }
                        else
                        {
                            string[] songs = new string[] { allSongs };
                            playlist = new Playlist(playlistName, songs);
                        }
                    }
                }
                playlists.Add(playlistName, playlist);
            }
        }
        catch
        {
            MessageBox.Show("Unable to load playlists.");
        }
    }

    private void LoadSettings()
    {
        VolumeSlider.Value = Properties.Settings.Default.PlayerVolume;
        mediaPlayer.Volume = Properties.Settings.Default.PlayerVolume / 100f;
        VolumeLabel.Content = $"Volume: {VolumeSlider.Value}%";

        LoopToggleButton.IsChecked = Properties.Settings.Default.PlayerLoop;
        ShuffleToggleButton.IsChecked = Properties.Settings.Default.PlayerShuffle;
    }

    private void Timer_Tick(object sender, EventArgs e)
    {
        if (mediaPlayer.Source == null) return;
        if (!mediaAvailable) return;
        if (!isProgressSliderBeingDragged)
        {
            ProgressSlider.Value = mediaPlayer.Position.TotalSeconds;
        }
        if (!mediaPlayer.NaturalDuration.HasTimeSpan) return;
        ProgressLabel.Content = $"{mediaPlayer.Position.Minutes}:{mediaPlayer.Position.Seconds.ToString().PadLeft(2, '0')} / {mediaPlayer.NaturalDuration.TimeSpan.Minutes}:{mediaPlayer.NaturalDuration.TimeSpan.Seconds.ToString().PadLeft(2, '0')}";
    }

    public void PlaySong(string songFile, SongCollection? songPlaylist)
    {
        if (songPlaylist is null) return;
        mediaPlayer.Close();
        mediaPlayer.Stop();

        if (!File.Exists(songFile)) return;

        Song song = songPlaylist.Songs[songFile];
        if (song.Title != string.Empty)
        {
            CurrentSongTitleLabel.Content = song.Title;
        }
        else
        {
            CurrentSongTitleLabel.Content = "Generic song";
        }
        CurrentSongArtistAlbumLabel.Content = song.Artist;
        if (!song.Album.Equals(Album.Empty))
        {
            CurrentSongArtistAlbumLabel.Content += " - " + song.Artist;
        }
        else
        {
            CurrentSongArtistAlbumLabel.Content = "Generic artist - Generic album";
        }

        mediaPlayer.Open(new Uri(songFile));
        mediaPlayer.Play();
        mediaPlayer.Volume = VolumeSlider.Value / 100f;
        currentCollection = songPlaylist;
        currentSong = song;
        ProgressSlider.Value = 0;
        playedSongs.Add(new Tuple<string, SongCollection>(song.FileName, currentCollection));

        LoadLyricsInView();
    }

    private void PreviousSongInPlaylist()
    {
        if (playedSongs.Count < 2) return;
        Tuple<string, SongCollection> previousEntry = playedSongs[playedSongs.Count - 2];
        playedSongs.RemoveAt(playedSongs.Count - 2);
        PlaySong(previousEntry.Item1, previousEntry.Item2);
    }

    private void NextSongInPlaylist()
    {
        int randomNextSongIndex = new Random().Next(currentCollection!.Songs.Count);
        PlaySong(currentCollection.Songs[currentCollection.Songs.Keys.ElementAt(randomNextSongIndex)].FilePath, currentCollection!);
    }

    private void MediaPlayer_MediaOpened(object? sender, EventArgs e)
    {
        ProgressSlider.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
        ProgressSlider.Value = 0;
        mediaAvailable = true;
        IsPlaying = true;
    }

    private void MediaPlayer_MediaEnded(object? sender, EventArgs e)
    {
        mediaAvailable = false;
        mediaPlayer.Close();
        mediaPlayer.Stop();
        IsPlaying = false;
        GC.Collect();

        if (currentSong is null) return;
        if ((bool)LoopToggleButton.IsChecked!)
        {
            PlaySong(currentSong.Value.FileName, currentCollection!);
        }
        else if ((bool)ShuffleToggleButton.IsChecked!)
        {
            NextSongInPlaylist();
        }
    }

    private void MediaPlayer_MediaFailed(object? sender, EventArgs e)
    {
        mediaAvailable = false;
        IsPlaying = false;
    }

    private void SetSongLyricsRichTextBoxText(string text)
    {
        FlowDocument doc = new FlowDocument();
        Run run = new Run(text);
        doc.Blocks.Add(new Paragraph(run));
        SongLyricsRichTextBox.Document = doc;
    }

    private void SetPlayPauseImage(bool playing)
    {
        BitmapImage image = new BitmapImage();
        image.BeginInit();
        image.UriSource = new Uri(playing ? "/Images/pause.png" : "/Images/play.png", UriKind.Relative);
        image.EndInit();

        PlayPauseButtonImage.Source = image;
    }

    private void IncreaseVolume()
    {
        mediaPlayer.Volume += (double)VOLUME_STEP / 100f;
        VolumeSlider.Value += VOLUME_STEP;
        VolumeLabel.Content = $"Volume: {VolumeSlider.Value}%";
        Properties.Settings.Default.PlayerVolume = VolumeSlider.Value;
    }

    private void DecreaseVolume()
    {
        mediaPlayer.Volume -= (double)VOLUME_STEP / 100f;
        VolumeSlider.Value -= VOLUME_STEP;
        VolumeLabel.Content = $"Volume: {VolumeSlider.Value}%";
        Properties.Settings.Default.PlayerVolume = VolumeSlider.Value;
    }

    private void ToggleLoop()
    {
        LoopToggleButton.IsChecked = !LoopToggleButton.IsChecked;
        Properties.Settings.Default.PlayerLoop = (bool)LoopToggleButton.IsChecked!;
    }

    private void ToggleShuffle()
    {
        ShuffleToggleButton.IsChecked = !ShuffleToggleButton.IsChecked;
        Properties.Settings.Default.PlayerShuffle = (bool)ShuffleToggleButton.IsChecked!;
    }

    public void OnWindowKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.F12)
        {
            new SussyWindow().Show();
        }
    }
}
