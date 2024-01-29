﻿using System;
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
    public Song? currentSong = null;
    private PlaylistElement? currentlySelectedPlaylistElement;
    private CollectionElement? currentlySelectedAlbumElement;
    private CollectionElement? currentlySelectedArtistElement;

    private SongCollection? currentCollection;
    private Playlist allSongsPlaylist;
    private Dictionary<string, Playlist> playlists = new Dictionary<string, Playlist>();
    private Dictionary<string, Album> albums = new Dictionary<string, Album>();
    private Dictionary<string, Playlist> artists = new Dictionary<string, Playlist>();
    private List<Tuple<Song, SongCollection>> playedSongs = new List<Tuple<Song, SongCollection>>();

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
        LoadAllAlbums();
        //Carichiamo tutti gli artisti
        LoadAllArtists();
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
        AlbumsView.Visibility = Visibility.Collapsed;
        ArtistsView.Visibility = Visibility.Collapsed;
        PlaylistsView.Visibility = Visibility.Collapsed;
        SearchResultsView.Visibility = Visibility.Collapsed;
        AllSongsView.Visibility = Visibility.Visible;
    }

    public void SwitchToPlaylistsView(object sender, RoutedEventArgs e)
    {
        AllSongsView.Visibility = Visibility.Collapsed;
        AlbumsView.Visibility = Visibility.Collapsed;
        ArtistsView.Visibility = Visibility.Collapsed;
        SearchResultsView.Visibility = Visibility.Collapsed;
        PlaylistsView.Visibility = Visibility.Visible;
    }

    public void SwitchToAlbumsView(object sender, RoutedEventArgs e)
    {
        AllSongsView.Visibility = Visibility.Collapsed;
        ArtistsView.Visibility = Visibility.Collapsed;
        SearchResultsView.Visibility = Visibility.Collapsed;
        PlaylistsView.Visibility = Visibility.Collapsed;
        AlbumsView.Visibility = Visibility.Visible;
    }

    public void SwitchToArtistsView(object sender, RoutedEventArgs e)
    {
        AllSongsView.Visibility = Visibility.Collapsed;
        AlbumsView.Visibility = Visibility.Collapsed;
        SearchResultsView.Visibility = Visibility.Collapsed;
        PlaylistsView.Visibility = Visibility.Collapsed;
        ArtistsView.Visibility = Visibility.Visible;
    }

    public void SwitchToSearchResultsView(object sender, RoutedEventArgs e)
    {
        AllSongsView.Visibility = Visibility.Collapsed;
        PlaylistsView.Visibility = Visibility.Collapsed;
        AlbumsView.Visibility = Visibility.Collapsed;
        ArtistsView.Visibility = Visibility.Collapsed;
        SearchResultsView.Visibility = Visibility.Visible;
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
            if (playlists.ContainsKey(name))
            {
                MessageBox.Show($"A playlist called '{name}' already exists!");
                return;
            }
            string path = $"{PLAYLISTS_PATH}\\{name}.homppl";
            try
            {
                using (StreamWriter sw = new StreamWriter(path))
                {
                    sw.Write("");
                    sw.Close();
                    sw.Dispose();
                }
                playlists.Add(name, new Playlist(name));
                PlaylistsListPanel.Children.Add(new CollectionElement((self) =>
                {
                    LoadPlaylistSongsInView(playlists[name]);
                })
                {
                    Text = name
                });
            }
            catch
            {
                MessageBox.Show("An error occurred while trying to create the new playlist.");
            }
        }
    }

    public void RenamePlaylist(Playlist playlist)
    {
        if (playlist.Equals(Playlist.Empty)) return;
        string oldPath = $"{PLAYLISTS_PATH}\\{playlist.Name}.homppl";
        InputBox ib = new InputBox("Insert playlist name", "Type the new name you want to give to the playlist:");
        if (ib.ShowDialog() == true)
        {
            string newName = ib.InputTextBox.Text;
            string newPath = $"{PLAYLISTS_PATH}\\{newName}.homppl";
            try
            {
                File.Move(oldPath, newPath);

                LoadAllPlaylists();
            }
            catch
            {
                MessageBox.Show("An error occurred while trying to rename the playlist.");
            }
        }
    }

    public void DeletePlaylist(Playlist playlist)
    {
        if (playlist.Equals(Playlist.Empty)) return;
        string path = $"{PLAYLISTS_PATH}\\{playlist.Name}.homppl";
        if (MessageBox.Show($"Are you sure to delete the playlist \"{playlist.Name}\"?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        {
            try
            {
                File.Delete(path);

                LoadAllPlaylists();
            }
            catch
            {
                MessageBox.Show("An error occurred while trying to delete the selected playlist.");
            }
        }
    }

    //TODO: Replace OpenFileDialog with a custom dialog from where the user can choose songs from the all songs list.
    public void AddSongsToPlaylist(Playlist playlist)
    {
        if (playlist.Equals(Playlist.Empty)) return;
        string path = $"{PLAYLISTS_PATH}\\{playlist.Name}.homppl";

        OpenFileDialog ofd = new OpenFileDialog();
        ofd.Multiselect = true;
        ofd.InitialDirectory = MUSIC_PATH;
        ofd.Title = $"Select the songs to add to the playlist \"{playlist.Name}\"...";
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
                    }
                    sw.Close();
                    sw.Dispose();
                }
                LoadAllPlaylists();
            }
            catch
            {
                MessageBox.Show("An error occurred while adding the selected songs to the playlist.");
            }
        }
    }

    public void RemoveSongsFromPlaylistButtonClick(object sender, RoutedEventArgs e)
    {
        //if (PlaylistsListView.SelectedItem == null) return;
        //string playlistName = PlaylistsListView.SelectedItem.ToString()!;
        //string playlistPath = $"{PLAYLISTS_PATH}\\{playlistName}.homppl";

        //if (PlaylistsSongsListView.SelectedItem == null) return;
        //string songName = PlaylistsSongsListView.SelectedItem.ToString()!;
        //string songPath = $"{MUSIC_PATH}\\{songName}.mp3";

        //if (MessageBox.Show($"Are you sure to remove the song \"{songName}\" from the playlist?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
        //{
        //    try
        //    {
        //        using (StreamReader sr = new StreamReader(playlistPath))
        //        {
        //            string playlistContent = sr.ReadToEnd();
        //            sr.Close();
        //            sr.Dispose();
        //            using (StreamWriter sw = new StreamWriter(playlistPath))
        //            {
        //                sw.Write(playlistContent.Replace($"{songPath}|", ""));
        //                sw.Close();
        //                sw.Dispose();
        //            }
        //        }
        //        PlaylistsSongsListView.Items.Remove(songName);
        //        playlists[playlistName].RemoveSong(songName);
        //    }
        //    catch
        //    {
        //        MessageBox.Show("An error occurred while removing the song from the playlist.");
        //    }
        //}
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

    public void LoadPlaylistSongsInView(Playlist playlist)
    {
        if (playlist.Equals(Playlist.Empty)) return;
        PlaylistSongsListPanel.Children.Clear();
        PlaylistSongsListPanel.Children.Add(new Label() { Content = playlist.Name, Foreground = Brushes.WhiteSmoke, FontSize = 18, FontWeight = FontWeights.Bold });
        foreach (Song song in playlists[playlist.Name].Songs.Values)
        {
            PlaylistSongsListPanel.Children.Add(new CustomSongElement()
            {
                Text = song.FileName,
                Song = song,
                Collection = playlists[playlist.Name]
            });
        }
    }

    public void LoadAlbumSongsInView(string albumName)
    {
        if (!albums.ContainsKey(albumName)) { MessageBox.Show("Could not load album."); return; }
        AlbumSongsListPanel.Children.Clear();
        AlbumSongsListPanel.Children.Add(new Label() { Content = albumName, Foreground = Brushes.WhiteSmoke, FontSize = 18, FontWeight = FontWeights.Bold });
        foreach (Song song in albums[albumName].Songs.Values)
        {
            AlbumSongsListPanel.Children.Add(new CustomSongElement()
            {
                Text = song.FileName,
                Song = song,
                Collection = albums[albumName]
            });
        }
    }

    public void LoadArtistSongsInView(string artistName)
    {
        if (!artists.ContainsKey(artistName)) { MessageBox.Show("Could not load artist's songs."); return; }
        ArtistSongsListPanel.Children.Clear();
        ArtistSongsListPanel.Children.Add(new Label() { Content = artistName, Foreground = Brushes.WhiteSmoke, FontSize = 18, FontWeight = FontWeights.Bold });
        foreach (Song song in artists[artistName].Songs.Values)
        {
            ArtistSongsListPanel.Children.Add(new CustomSongElement()
            {
                Text = song.FileName,
                Song = song,
                Collection = artists[artistName]
            });
        }
    }

    public void SearchInputTextBoxKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            IEnumerable<Song> results = from song in allSongsPlaylist.Songs.Values
                                        where song.IsCorrelated(SearchInputTextBox.Text)
                                        select song;
            SearchResultSongsListPanel.Children.Clear();
            StackPanel spanel = new StackPanel() { Orientation = Orientation.Vertical };
            spanel.Children.Add(new Label()
            {
                Content = $"Search results for '{SearchInputTextBox.Text}'",
                Foreground = Brushes.WhiteSmoke,
                FontSize = 18,
                FontWeight = FontWeights.Bold,
                Margin = new Thickness(0),
                Padding = new Thickness(0)
            });
            spanel.Children.Add(new Label()
            {
                Content = $"{results.Count()} results",
                Foreground = Brushes.LightGray,
                FontSize = 14,
                Margin = new Thickness(0),
                Padding = new Thickness(0)
            });
            SearchResultSongsListPanel.Children.Add(spanel);
            foreach (Song song in results)
            {
                SearchResultSongsListPanel.Children.Add(new CustomSongElement()
                {
                    Text = song.FileName,
                    Song = song,
                    Collection = allSongsPlaylist
                });
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
            AllSongsListPanel.Children.Clear();
            AllSongsListPanel.Children.Add(new Label() { Content = "All Songs", Foreground = Brushes.WhiteSmoke, FontSize = 18, FontWeight = FontWeights.Bold });
            foreach (string songPath in songPaths)
            {
                Song song = new Song(songPath);
                try
                {
                    song.FileName = songPath.Replace($"{MUSIC_PATH}\\", "").Replace(".mp3", "");
                    props?.Clear();
                    props = Utils.ReadID3v2Tags(songPath);
                    string title = songPath;
                    string artistName = "Unknown Artist";
                    string albumName = "Unknown Album";
                    string year = "";
                    if (props is not null)
                    {
                        // Title
                        if (props.ContainsKey("TIT2")) title = props["TIT2"];
                        song.Title = title;

                        // Artist
                        if (props.ContainsKey("TPE1")) artistName = props["TPE1"];
                        song.Artist = artistName;
                        if (!artists.ContainsKey(artistName))
                        {
                            artists[artistName] = new Playlist(artistName);
                        }

                        // Year
                        if (props.ContainsKey("TYER"))
                        {
                            year = props["TYER"];
                        }
                        else
                        {
                            if (props.ContainsKey("TDRC")) year = props["TDRC"];
                        }
                        song.Year = year;

                        // Album
                        if (props.ContainsKey("TALB")) albumName = props["TALB"];
                        if (!albums.ContainsKey(albumName))
                        {
                            albums[albumName] = new Album(albumName);
                        }
                        song.Album = albums[albumName];
                    }

                    allSongsPlaylist.AddSong(song);
                    artists[artistName].AddSong(song);
                    albums[albumName].AddSong(song);

                    AllSongsListPanel.Children.Add(new CustomSongElement()
                    {
                        Text = song.FileName,
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
            playlists.Clear();
            PlaylistsListPanel.Children.Clear();
            PlaylistsListPanel.Children.Add(new Label() { Content = "Playlists", Foreground = Brushes.WhiteSmoke, FontSize = 18, FontWeight = FontWeights.Bold });
            foreach (string p in allPlaylists)
            {
                string playlistName = p.Replace($"{PLAYLISTS_PATH}\\", "").Replace(".homppl", "");
                Playlist playlist = new Playlist(playlistName);
                string[] songs = new string[] { };
                using (StreamReader sr = new StreamReader(p))
                {
                    string allSongs = sr.ReadToEnd().Trim();
                    if (!string.IsNullOrEmpty(allSongs) && !string.IsNullOrWhiteSpace(allSongs))
                    {
                        songs = allSongs.Contains("|") ? allSongs.Split("|") : new string[] { allSongs };
                    }
                    sr.Close();
                }

                foreach (string song in songs)
                {
                    if (!allSongsPlaylist.Songs.ContainsKey(song)) continue;
                    playlist.AddSong(allSongsPlaylist.Songs[song]);
                }

                PlaylistElement playlistElement = new PlaylistElement(
                    (self) =>
                    {
                        if (currentlySelectedPlaylistElement is not null) currentlySelectedPlaylistElement.Focused = false;
                        LoadPlaylistSongsInView(playlists[playlistName]);
                        currentlySelectedPlaylistElement = self;
                    },
                    () => { AddSongsToPlaylist(playlist); },
                    () => { RenamePlaylist(playlist); },
                    () => { DeletePlaylist(playlist); }
                )
                {
                    Text = playlistName,
                };
                PlaylistsListPanel.Children.Add(playlistElement);
                playlists.Add(playlistName, playlist);
            }
        }
        catch
        {
            MessageBox.Show("Unable to load playlists.");
        }
    }

    private void LoadAllAlbums()
    {
        AlbumsListPanel.Children.Clear();
        AlbumsListPanel.Children.Add(new Label() { Content = "Albums", Foreground = Brushes.WhiteSmoke, FontSize = 18, FontWeight = FontWeights.Bold });
        foreach (KeyValuePair<string, Album> kvp in albums.OrderBy((kvp) => kvp.Key))
        {
            AlbumsListPanel.Children.Add(new CollectionElement((self) =>
            {
                if (currentlySelectedAlbumElement is not null) currentlySelectedAlbumElement.Focused = false;
                LoadAlbumSongsInView(kvp.Key);
                currentlySelectedAlbumElement = self;
            })
            {
                Text = kvp.Key
            });
        }
    }

    private void LoadAllArtists()
    {
        ArtistsListPanel.Children.Clear();
        ArtistsListPanel.Children.Add(new Label() { Content = "Artists", Foreground = Brushes.WhiteSmoke, FontSize = 18, FontWeight = FontWeights.Bold });
        foreach (KeyValuePair<string, Playlist> kvp in artists.OrderBy((kvp) => kvp.Key))
        {
            ArtistsListPanel.Children.Add(new CollectionElement((self) =>
            {
                if (currentlySelectedArtistElement is not null) currentlySelectedArtistElement.Focused = false;
                LoadArtistSongsInView(kvp.Key);
                currentlySelectedArtistElement = self;
            })
            {
                Text = kvp.Key
            });
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

        if (!File.Exists(songFile))
        {
            MessageBox.Show($"Song file does not exist: {songFile}");
            return;
        }

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
            CurrentSongArtistAlbumLabel.Content += " - " + song.Album.Name;
        }

        mediaPlayer.Open(new Uri(songFile));
        mediaPlayer.Play();
        mediaPlayer.Volume = VolumeSlider.Value / 100f;
        currentCollection = songPlaylist;
        currentSong = song;
        ProgressSlider.Value = 0;
        playedSongs.Add(new Tuple<Song, SongCollection>(song, currentCollection));

        LoadLyricsInView();
    }

    private void PreviousSongInPlaylist()
    {
        if (playedSongs.Count < 2) return;
        Tuple<Song, SongCollection> previousEntry = playedSongs[playedSongs.Count - 2];
        playedSongs.RemoveAt(playedSongs.Count - 2);
        PlaySong(previousEntry.Item1.FilePath, previousEntry.Item2);
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
}
