using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Windows.Media.Imaging;
using System.Collections.Specialized;
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
    private Random random = new Random();
    private bool isPlaying = false;
    private bool IsPlaying
    {
        get { return isPlaying; }
        set
        {
            isPlaying = value;
            if (value)
            {
                PlayerFadeIn();
                SetPlayPauseImage(true);
                MiniPlayerWindow.Instance?.SetPlayPauseImage(true);
            }
            else
            {
                SetPlayPauseImage(false);
                MiniPlayerWindow.Instance?.SetPlayPauseImage(false);
            }
        }
    }
    private DispatcherTimer fadeTimer = new DispatcherTimer();
    private int fadeNumIterations = 100;
    private int fadeIterations = 0;
    private bool isFadingOut = false;
    private bool mediaAvailable = false;
    private bool isProgressSliderBeingDragged = false;
    private bool isVolumeSliderBeingDragged = false;
    private bool isSettingsMiniplayerOpacitySliderBeingDragged = false;
    public Song? currentSong = null;
    private Song? prioritySong = null;
    private PlaylistElement? currentlySelectedPlaylistElement;
    private CollectionElement? currentlySelectedAlbumElement;
    private CollectionElement? currentlySelectedArtistElement;

    private SongCollection? currentCollection;
    private Playlist allSongsPlaylist;
    private Dictionary<string, Playlist> playlists = new Dictionary<string, Playlist>();
    private Dictionary<string, Album> albums = new Dictionary<string, Album>();
    private Dictionary<string, Playlist> artists = new Dictionary<string, Playlist>();
    private List<Song> playedSongs = new List<Song>();

    public MainWindow()
    {
        InitializeComponent();

        Instance = this;
#if DEBUG
        Title = $"(DEBUG) {Title}";
#endif

        KeyboardHook.OnKeyPressed += HandleHotkey;
        KeyboardHook.Start();
    }

    public async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        //Carichiamo le impostazioni
        LoadSettings();
        //Carichiamo tutte le canzoni
        await LoadAllSongs();
        //Carichiamo tutte le playlist
        LoadAllPlaylists();
        //Carichiamo tutti gli album
        LoadAllAlbums();
        //Carichiamo tutti gli artisti
        LoadAllArtists();

        mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
        mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;

        timer.Interval = TimeSpan.FromMilliseconds(100);
        timer.Tick += Timer_Tick;
        timer.Start();
    }

    private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Properties.Settings.Default.Save();
        KeyboardHook.Stop();
    }

    private void HandleHotkey(object? sender, Key key)
    {
        if (Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Alt | ModifierKeys.Shift))
        {
            if (key == (Key)Properties.Settings.Default.PlayPauseShortcutKey)
            {
                TogglePlayPause();
            }
            else if (key == (Key)Properties.Settings.Default.IncreaseVolumeShortcutKey)
            {
                IncreaseVolume();
            }
            else if (key == (Key)Properties.Settings.Default.DecreaseVolumeShortcutKey)
            {
                DecreaseVolume();
            }
            else if (key == (Key)Properties.Settings.Default.NextSongShortcutKey)
            {
                NextSongInPlaylist();
            }
            else if (key == (Key)Properties.Settings.Default.PreviousSongShortcutKey)
            {
                PreviousSongInPlaylist();
            }
            else if (key == (Key)Properties.Settings.Default.ToggleLoopShortcutKey)
            {
                ToggleLoop();
            }
            else if (key == (Key)Properties.Settings.Default.ToggleShuffleShortcutKey)
            {
                ToggleShuffle();
            }
        }
    }

    private void SwitchToAllSongsView(object sender, RoutedEventArgs e)
    {
        AlbumsView.Visibility = Visibility.Collapsed;
        ArtistsView.Visibility = Visibility.Collapsed;
        PlaylistsView.Visibility = Visibility.Collapsed;
        SearchResultsView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Collapsed;
        AllSongsView.Visibility = Visibility.Visible;
    }

    private void SwitchToPlaylistsView(object sender, RoutedEventArgs e)
    {
        AllSongsView.Visibility = Visibility.Collapsed;
        AlbumsView.Visibility = Visibility.Collapsed;
        ArtistsView.Visibility = Visibility.Collapsed;
        SearchResultsView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Collapsed;
        PlaylistsView.Visibility = Visibility.Visible;
    }

    private void SwitchToAlbumsView(object sender, RoutedEventArgs e)
    {
        AllSongsView.Visibility = Visibility.Collapsed;
        ArtistsView.Visibility = Visibility.Collapsed;
        SearchResultsView.Visibility = Visibility.Collapsed;
        PlaylistsView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Collapsed;
        AlbumsView.Visibility = Visibility.Visible;
    }

    private void SwitchToArtistsView(object sender, RoutedEventArgs e)
    {
        AllSongsView.Visibility = Visibility.Collapsed;
        AlbumsView.Visibility = Visibility.Collapsed;
        SearchResultsView.Visibility = Visibility.Collapsed;
        PlaylistsView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Collapsed;
        ArtistsView.Visibility = Visibility.Visible;
    }

    private void SwitchToSearchResultsView(object sender, RoutedEventArgs e)
    {
        AllSongsView.Visibility = Visibility.Collapsed;
        PlaylistsView.Visibility = Visibility.Collapsed;
        AlbumsView.Visibility = Visibility.Collapsed;
        ArtistsView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Collapsed;
        SearchResultsView.Visibility = Visibility.Visible;
    }

    private void SwitchToSettingsView(object sender, RoutedEventArgs e)
    {
        AllSongsView.Visibility = Visibility.Collapsed;
        PlaylistsView.Visibility = Visibility.Collapsed;
        AlbumsView.Visibility = Visibility.Collapsed;
        ArtistsView.Visibility = Visibility.Collapsed;
        SearchResultsView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Visible;
    }

    private void PlayPauseButtonClick(object sender, RoutedEventArgs e)
    {
        TogglePlayPause();
    }

    public void TogglePlayPause()
    {
        if (IsPlaying)
        {
            if (mediaPlayer.CanPause) mediaPlayer.Pause();
            IsPlaying = false;
        }
        else
        {
            if (mediaPlayer.Source is null)
            {
                currentCollection = allSongsPlaylist;
                NextSongInPlaylist();
            }
            mediaPlayer.Play();
            IsPlaying = true;
        }
    }

    private void ProgressSliderMouseMove(object sender, RoutedEventArgs e)
    {
        if (!isProgressSliderBeingDragged) return;
        mediaPlayer.Position = TimeSpan.FromSeconds(ProgressSlider.Value);
    }

    private void ProgressSliderMouseDown(object sender, RoutedEventArgs e)
    {
        isProgressSliderBeingDragged = true;
    }

    private void ProgressSliderMouseUp(object sender, RoutedEventArgs e)
    {
        isProgressSliderBeingDragged = false;
    }

    private void VolumeSliderMouseMove(object sender, RoutedEventArgs e)
    {
        if (!isVolumeSliderBeingDragged) return;
        mediaPlayer.Volume = VolumeSlider.Value / 100f;
        Properties.Settings.Default.PlayerVolume = VolumeSlider.Value;
        VolumeLabel.Content = $"Volume: {VolumeSlider.Value}%";
    }

    private void VolumeSliderMouseDown(object sender, RoutedEventArgs e)
    {
        isVolumeSliderBeingDragged = true;
    }

    private void VolumeSliderMouseUp(object sender, RoutedEventArgs e)
    {
        isVolumeSliderBeingDragged = false;
    }

    private void VolumeSliderMouseWheel(object sender, MouseWheelEventArgs e)
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

    private void LoopToggleButtonClick(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.PlayerLoop = (bool)LoopToggleButton.IsChecked!;
    }

    private void ShuffleToggleButtonClick(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.PlayerShuffle = (bool)ShuffleToggleButton.IsChecked!;
    }

    private void BackwardButtonClick(object sender, RoutedEventArgs e)
    {
        PreviousSongInPlaylist();
    }

    private void ForwardButtonClick(object sender, RoutedEventArgs e)
    {
        NextSongInPlaylist();
    }

    private void NewPlaylistButtonClick(object sender, RoutedEventArgs e)
    {
        InputBox ib = new InputBox("Insert playlist name", "Type the name you want to give to the playlist:");
        if (ib.ShowDialog() == true)
        {
            string name = ib.InputTextBox.Text.Trim();
            if (playlists.ContainsKey(name))
            {
                MessageBox.Show($"A playlist called '{name}' already exists!");
                return;
            }
            if (string.IsNullOrEmpty(name))
            {
                MessageBox.Show($"You need to specify a valid name for the playlist.");
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
                PlaylistsListPanel.Children.Add(new PlaylistElement(
                    (self) =>
                    {
                        LoadPlaylistSongsInView(playlists[name]);
                    },
                    (self) =>
                    {
                        PlayCollection(playlists[name]);
                    },
                    () => { ManagePlaylistSongs(playlists[name]); },
                    () => { RenamePlaylist(playlists[name]); },
                    () => { DeletePlaylist(playlists[name]); }
                )
                {
                    Text = name,
                });
            }
            catch
            {
                MessageBox.Show("An error occurred while trying to create the new playlist.");
            }
        }
    }

    private void RenamePlaylist(Playlist playlist)
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

    private void DeletePlaylist(Playlist playlist)
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

    private void ManagePlaylistSongs(Playlist playlist)
    {
        if (playlist.Equals(Playlist.Empty)) return;
        SongsChooserDialog scd = new SongsChooserDialog(allSongsPlaylist, playlist);
        if (scd.ShowDialog() != true) return;
        var result = scd.Result;
        string path = $"{PLAYLISTS_PATH}\\{playlist.Name}.homppl";
        try
        {
            using (StreamWriter sw = new StreamWriter(path, false))
            {
                foreach (Song song in result)
                {
                    sw.Write($"{song.FilePath}|");
                }
                sw.Close();
            }
            LoadAllPlaylists();
        }
        catch
        {
            MessageBox.Show("An error occurred while saving changes to the playlist.");
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

    private void LoadPlaylistSongsInView(Playlist playlist)
    {
        if (playlist.Equals(Playlist.Empty)) return;
        PlaylistSongsListPanel.Children.Clear();
        PlaylistSongsListPanel.Children.Add(new Label() { Content = playlist.Name, Foreground = Brushes.WhiteSmoke, FontSize = 18, FontWeight = FontWeights.Bold });
        foreach (KeyValuePair<string, Song> songPair in playlists[playlist.Name].Songs.OrderBy((kvp) => kvp.Key))
        {
            PlaylistSongsListPanel.Children.Add(new CustomSongElement()
            {
                Text = songPair.Value.FileName,
                Song = songPair.Value,
                Collection = playlists[playlist.Name]
            });
        }
    }

    private void LoadAlbumSongsInView(string albumName)
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

    private void LoadArtistSongsInView(string artistName)
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

    private void SearchInputTextBoxKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            IEnumerable<Song> results = from song in allSongsPlaylist.Songs.Values
                                        where song.IsCorrelated(SearchInputTextBox.Text)
                                        select song;
            Playlist playlist = allSongsPlaylist;
            if (Properties.Settings.Default.UseSearchResultsAsShuffleSource)
            {
                playlist = new Playlist("__HOMP_SEARCH_RESULTS_PLAYLIST__");
                foreach (Song song in results)
                {
                    playlist.AddSong(song);
                }
            }
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
                    Collection = playlist
                });
            }
            SwitchToSearchResultsView(null!, null!);
        }
    }

    private async Task<bool> LoadAllSongs()
    {
        allSongsPlaylist = new Playlist("__HOMP_ALL_SONGS_PLAYLIST__");
        AllSongsListPanel.Children.Clear();
        AllSongsListPanel.Children.Add(new Label() { Content = "All Songs", Foreground = Brushes.WhiteSmoke, FontSize = 18, FontWeight = FontWeights.Bold });
        foreach (string? path in Properties.Settings.Default.SourceDirectories)
        {
            if (path is null) continue;
            if (!Directory.Exists(path)) continue;
            try
            {
                string[] songPaths = Directory.GetFiles(path, "*.mp3");
                foreach (string songPath in songPaths)
                {
                    await LoadSong(path, songPath);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unable to load songs from directory '{path}'.\n\n" + ex.Message + "\n" + ex.StackTrace + ex.InnerException?.Message);
                return false;
            }
        }
        return true;
    }

    private void LoadAllPlaylists()
    {
        if (!Directory.Exists(PLAYLISTS_PATH)) return;
        try
        {
            string[] allPlaylists = Directory.GetFiles(PLAYLISTS_PATH, "*.homppl");
            playlists.Clear();
            PlaylistsListPanel.Children.Clear();
            PlaylistSongsListPanel.Children.Clear();

            Grid grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(100, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition() { Width = new GridLength(0, GridUnitType.Auto) });

            Label titleLabel = new Label() { Content = "Playlists", Foreground = Brushes.WhiteSmoke, FontSize = 18, FontWeight = FontWeights.Bold };
            Grid.SetColumn(titleLabel, 0);
            grid.Children.Add(titleLabel);

            Button addNewButton = new Button() { Content = "New playlist...", Height = 30d, Style = FindResource("CustomButton") as Style };
            addNewButton.Click += NewPlaylistButtonClick;
            Grid.SetColumn(addNewButton, 2);
            grid.Children.Add(addNewButton);

            PlaylistsListPanel.Children.Add(grid);
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
                    (self) =>
                    {
                        PlayCollection(playlist);
                    },
                    () => { ManagePlaylistSongs(playlist); },
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
            AlbumsListPanel.Children.Add(new CollectionElement(
            (self) =>
            {
                if (currentlySelectedAlbumElement is not null) currentlySelectedAlbumElement.Focused = false;
                LoadAlbumSongsInView(kvp.Key);
                currentlySelectedAlbumElement = self;
            },
            (self) =>
            {
                PlayCollection(kvp.Value);
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
            ArtistsListPanel.Children.Add(new CollectionElement(
            (self) =>
            {
                if (currentlySelectedArtistElement is not null) currentlySelectedArtistElement.Focused = false;
                LoadArtistSongsInView(kvp.Key);
                currentlySelectedArtistElement = self;
            },
            (self) =>
            {
                PlayCollection(kvp.Value);
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

        SettingsSearchShuffleCheckbox.IsChecked = Properties.Settings.Default.UseSearchResultsAsShuffleSource;

        SettingsPlayPauseShortcutButton.Content = $"{(Key)Properties.Settings.Default.PlayPauseShortcutKey}";
        SettingsIncreaseVolumeShortcutButton.Content = $"{(Key)Properties.Settings.Default.IncreaseVolumeShortcutKey}";
        SettingsDecreaseVolumeShortcutButton.Content = $"{(Key)Properties.Settings.Default.DecreaseVolumeShortcutKey}";
        SettingsNextSongShortcutButton.Content = $"{(Key)Properties.Settings.Default.NextSongShortcutKey}";
        SettingsPreviousSongShortcutButton.Content = $"{(Key)Properties.Settings.Default.PreviousSongShortcutKey}";
        SettingsToggleLoopShortcutButton.Content = $"{(Key)Properties.Settings.Default.ToggleLoopShortcutKey}";
        SettingsToggleShuffleShortcutButton.Content = $"{(Key)Properties.Settings.Default.ToggleShuffleShortcutKey}";

        SettingsEnableFadeInCheckbox.IsChecked = Properties.Settings.Default.PlaybackFadeIn;
        SettingsEnableFadeOutCheckbox.IsChecked = Properties.Settings.Default.PlaybackFadeOut;

        if (Properties.Settings.Default.SourceDirectories is null || Properties.Settings.Default.SourceDirectories.Count == 0)
        {
            Properties.Settings.Default.SourceDirectories = new StringCollection { MUSIC_PATH };
        }
        foreach (string? sourcePath in Properties.Settings.Default.SourceDirectories)
        {
            if (sourcePath is null) continue;
            SettingsSourceDirectoriesListView.Items.Add(sourcePath);
        }

        SettingsMiniplayerAutoOpacityCheckbox.IsChecked = Properties.Settings.Default.MiniplayerAutoOpacity;
        SettingsMiniplayerOpacitySlider.Value = Properties.Settings.Default.MiniplayerMinimumOpacity;
        SettingsMiniplayerOpacitySliderLabel.Content = $"{(SettingsMiniplayerOpacitySlider.Value * 100d):0.00}%";
        SettingsMiniplayerOpacityTimeoutNumberInputBox.SetValue(Properties.Settings.Default.MiniplayerFadingTimeout);
    }

    private async Task<bool> LoadSong(string dirPath, string songPath)
    {
        Song song = new Song(songPath);
        Dictionary<string, string>? props = new();
        try
        {
            song.FileName = songPath.Replace($"{dirPath}\\", "").Replace(".mp3", "");
            props?.Clear();
            Tuple<bool, Dictionary<string, string>?> tagResult = Utils.ReadID3v2Tags(songPath);
            if (tagResult.Item1) props = tagResult.Item2;
            string title = song.FileName;
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

            await Dispatcher.BeginInvoke(() =>
            {
                AllSongsListPanel.Children.Add(new CustomSongElement()
                {
                    Text = song.FileName,
                    Song = song,
                    Collection = allSongsPlaylist,
                    HasErrored = false
                });
            }, DispatcherPriority.Background);

            return true;
        }
        catch
        {
            // No need to pass Song and Collection because an errored item
            // does not show a playing button.
            await Dispatcher.BeginInvoke(() =>
            {
                AllSongsListPanel.Children.Add(new CustomSongElement()
                {
                    Text = song.FileName,
                    HasErrored = true
                });
            }, DispatcherPriority.Background);

            return false;
        }
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (mediaPlayer.Source == null) return;
        if (!mediaAvailable) return;
        if (!isProgressSliderBeingDragged)
        {
            ProgressSlider.Value = mediaPlayer.Position.TotalSeconds;
        }
        if (!mediaPlayer.NaturalDuration.HasTimeSpan) return;
        if (mediaPlayer.Position.TotalMilliseconds >= mediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds - 1500)
        {
            if (!isFadingOut) PlayerFadeOut();
        }
        ProgressLabel.Content = $"{mediaPlayer.Position.Minutes}:{mediaPlayer.Position.Seconds.ToString().PadLeft(2, '0')} / {mediaPlayer.NaturalDuration.TimeSpan.Minutes}:{mediaPlayer.NaturalDuration.TimeSpan.Seconds.ToString().PadLeft(2, '0')}";
    }

    public void PlaySong(string songFile, SongCollection? songCollection)
    {
        if (songCollection is null) return;
        mediaPlayer.Close();
        mediaPlayer.Stop();

        if (!File.Exists(songFile))
        {
            MessageBox.Show($"Song file does not exist: {songFile}");
            return;
        }

        Song song = allSongsPlaylist.Songs[songFile];
        string title = (song.Title == string.Empty) switch
        {
            true => "Generic Song",
            false => song.Title
        };
        string artist = song.Artist;

        if (MiniPlayerWindow.Instance is not null)
        {
            MiniPlayerWindow.Instance.SetTitleText(title);
            MiniPlayerWindow.Instance.SetArtistText(artist);
        }

        if (!song.Album.Equals(Album.Empty))
        {
            artist += " - " + song.Album.Name;
        }
        CurrentSongTitleLabel.Content = title;
        CurrentSongArtistAlbumLabel.Content = artist;


        mediaPlayer.Open(new Uri(songFile));
        mediaPlayer.Play();
        mediaPlayer.Volume = VolumeSlider.Value / 100f;
        if (currentCollection != songCollection)
        {
            currentCollection = songCollection;
            playedSongs.Clear();
        }
        if (playedSongs.Count >= currentCollection.Songs.Count)
        {
            playedSongs.Clear();
        }
        currentSong = song;
        ProgressSlider.Value = 0;
        playedSongs.Add(song);

        LoadLyricsInView();
    }

    private void PreviousSongInPlaylist()
    {
        if (playedSongs.Count < 1) return;

        Song lastSong = playedSongs.Count switch
        {
            1 => playedSongs.Last(),
            _ => playedSongs[playedSongs.Count - 2]
        };
        playedSongs.Remove(lastSong);
        PlaySong(lastSong.FilePath, currentCollection);
    }

    public void NextSongInPlaylist()
    {
        if (prioritySong is not null)
        {
            PlaySong(prioritySong?.FilePath!, currentCollection ?? allSongsPlaylist);
            prioritySong = null;
            return;
        }

        if (currentCollection is null) return;
        if (playedSongs.Count >= currentCollection.Songs.Count)
        {
            playedSongs.Clear();
        }
        int randomNextSongIndex;
        do
        {
            randomNextSongIndex = random.Next(currentCollection.Songs.Count);
        }
        while (playedSongs.Contains(currentCollection.Songs.Values.ElementAt(randomNextSongIndex)));
        PlaySong(currentCollection.Songs[currentCollection.Songs.Keys.ElementAt(randomNextSongIndex)].FilePath, currentCollection);
    }

    private void MediaPlayer_MediaOpened(object? sender, EventArgs e)
    {
        if (!mediaPlayer.NaturalDuration.HasTimeSpan) return;
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
        if (prioritySong is not null)
        {
            PlaySong(prioritySong.Value.FilePath, currentCollection);
            prioritySong = null;
            return;
        }
        if ((bool)LoopToggleButton.IsChecked!)
        {
            PlaySong(currentSong.Value.FilePath, currentCollection!);
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

    private void OnSettingsSearchShuffleCheckboxChecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.UseSearchResultsAsShuffleSource = true;
        Properties.Settings.Default.Save();
    }

    private void OnSettingsSearchShuffleCheckboxUnchecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.UseSearchResultsAsShuffleSource = false;
        Properties.Settings.Default.Save();
    }

    private void ChangeShortcut(string settingName, Button button)
    {
        InputCaptureBox icb = new InputCaptureBox();
        if (icb.ShowDialog() == true)
        {
            Key key = icb.KeyResult;
            button.Content = $"{key}";
            Properties.Settings.Default[settingName] = (short)key;
        }
    }

    private void OnSettingsPlayPauseShortcutButtonClick(object sender, RoutedEventArgs e)
    {
        ChangeShortcut("PlayPauseShortcutKey", SettingsPlayPauseShortcutButton);
    }

    private void OnSettingsIncreaseVolumeShortcutButtonClick(object sender, RoutedEventArgs e)
    {
        ChangeShortcut("IncreaseVolumeShortcutKey", SettingsIncreaseVolumeShortcutButton);
    }

    private void OnSettingsDecreaseVolumeShortcutButtonClick(object sender, RoutedEventArgs e)
    {
        ChangeShortcut("DecreaseVolumeShortcutKey", SettingsDecreaseVolumeShortcutButton);
    }

    private void OnSettingsNextSongShortcutButtonClick(object sender, RoutedEventArgs e)
    {
        ChangeShortcut("NextSongShortcutKey", SettingsNextSongShortcutButton);
    }

    private void OnSettingsPreviousSongShortcutButtonClick(object sender, RoutedEventArgs e)
    {
        ChangeShortcut("PreviousSongShortcutKey", SettingsPreviousSongShortcutButton);
    }

    private void OnSettingsToggleLoopShortcutButtonClick(object sender, RoutedEventArgs e)
    {
        ChangeShortcut("ToggleLoopShortcutKey", SettingsToggleLoopShortcutButton);
    }

    private void OnSettingsToggleShuffleShortcutButtonClick(object sender, RoutedEventArgs e)
    {
        ChangeShortcut("ToggleShuffleShortcutKey", SettingsToggleShuffleShortcutButton);
    }

    private void OnSettingsEnableFadeInCheckboxChecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.PlaybackFadeIn = true;
    }

    private void OnSettingsEnableFadeInCheckboxUnchecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.PlaybackFadeIn = false;
    }

    private void OnSettingsEnableFadeOutCheckboxChecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.PlaybackFadeOut = true;
    }

    private void OnSettingsEnableFadeOutCheckboxUnchecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.PlaybackFadeOut = false;
    }

    private void PlayerFadeIn()
    {
        if (!Properties.Settings.Default.PlaybackFadeIn) return;
        mediaPlayer.Volume = 0d;
        fadeTimer.Interval = TimeSpan.FromMilliseconds(10);
        fadeTimer.Tick += PlayerFadeInEvent;
        fadeTimer.Start();
    }

    private void PlayerFadeInEvent(object? sender, EventArgs e)
    {
        if (isFadingOut) return;
        double finalVolume = Properties.Settings.Default.PlayerVolume / 100d;
        mediaPlayer.Volume += finalVolume / fadeNumIterations;
        fadeIterations++;
        if (fadeIterations == fadeNumIterations)
        {
            fadeTimer.Stop();
            fadeTimer.Tick -= PlayerFadeInEvent;
            fadeIterations = 0;
        }
    }

    private void PlayerFadeOut()
    {
        if (!Properties.Settings.Default.PlaybackFadeOut) return;
        fadeTimer.Interval = TimeSpan.FromMilliseconds(10);
        fadeTimer.Tick += PlayerFadeOutEvent;
        isFadingOut = true;
        fadeTimer.Start();
    }

    private void PlayerFadeOutEvent(object? sender, EventArgs e)
    {
        mediaPlayer.Volume -= mediaPlayer.Volume / fadeNumIterations;
        fadeIterations++;
        if (fadeIterations == fadeNumIterations)
        {
            fadeTimer.Stop();
            fadeTimer.Tick -= PlayerFadeOutEvent;
            fadeIterations = 0;
            isFadingOut = false;
        }
    }

    private void OnSettingsSourceDirectoriesAddButtonClick(object sender, RoutedEventArgs e)
    {
        using (System.Windows.Forms.FolderBrowserDialog fbd = new()
        {
            InitialDirectory = MUSIC_PATH,
            OkRequiresInteraction = true,
            ShowNewFolderButton = true,
            ShowPinnedPlaces = true
        })
        {
            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (!Directory.Exists(fbd.SelectedPath))
                {
                    MessageBox.Show("The selected directory does not exist.");
                    return;
                }
                if (Properties.Settings.Default.SourceDirectories.Contains(fbd.SelectedPath))
                {
                    MessageBox.Show("Cannot add the selected directory because it has been already added.");
                    return;
                }
                Properties.Settings.Default.SourceDirectories.Add(fbd.SelectedPath);
                SettingsSourceDirectoriesListView.Items.Add(fbd.SelectedPath);
            }
        }
    }

    private void OnSettingsSourceDirectoriesRemoveButtonClick(object sender, RoutedEventArgs e)
    {
        if (SettingsSourceDirectoriesListView.SelectedItem == null) return;
        string? dir = SettingsSourceDirectoriesListView.SelectedItem.ToString();
        if (dir is null) return;
        if (MessageBox.Show($"Are you sure you want to remove the directory '{dir}' from the sources directories?", "HOMP", MessageBoxButton.YesNo) == MessageBoxResult.Yes)
        {
            SettingsSourceDirectoriesListView.Items.Remove(dir);
            Properties.Settings.Default.SourceDirectories.Remove(dir);
        }
    }

    private void PlayCollection(SongCollection collection)
    {
        Song startingSong = collection.Songs.Values.ElementAt(random.Next(collection.Songs.Count));
        PlaySong(startingSong.FilePath, collection);
    }

    public void SetPrioritySong(Song song)
    {
        prioritySong = song;
    }

    private void MiniplayerButtonClick(object sender, RoutedEventArgs e)
    {
        new MiniPlayerWindow()?.Show();
        MiniPlayerWindow.Instance?.SetTitleText(currentSong?.Title ?? "Song title");
        MiniPlayerWindow.Instance?.SetArtistText(currentSong?.Artist ?? "Song artist");
        MiniPlayerWindow.Instance?.SetPlayPauseImage(IsPlaying);
        Hide();
    }

    private void OnSettingsMiniplayerAutoOpacityCheckboxChecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.MiniplayerAutoOpacity = true;
    }

    private void OnSettingsMiniplayerAutoOpacityCheckboxUnchecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.MiniplayerAutoOpacity = false;
    }

    private void SettingsMiniplayerOpacitySliderMouseMove(object sender, RoutedEventArgs e)
    {
        if (!isSettingsMiniplayerOpacitySliderBeingDragged) return;
        SettingsMiniplayerOpacitySliderLabel.Content = $"{(SettingsMiniplayerOpacitySlider.Value * 100d):0.00}%";
        Properties.Settings.Default.MiniplayerMinimumOpacity = SettingsMiniplayerOpacitySlider.Value;
    }

    private void SettingsMiniplayerOpacitySliderMouseDown(object sender, RoutedEventArgs e)
    {
        isSettingsMiniplayerOpacitySliderBeingDragged = true;
    }

    private void SettingsMiniplayerOpacitySliderMouseUp(object sender, RoutedEventArgs e)
    {
        isSettingsMiniplayerOpacitySliderBeingDragged = false;
    }

    private void SettingsMiniplayerOpacityTimeoutNumberInputBoxFinishedEditing(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.MiniplayerFadingTimeout = (int)SettingsMiniplayerOpacityTimeoutNumberInputBox.NumericValue;
    }
}
