using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Data.Sqlite;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Threading;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Collections.ObjectModel;
using CustomMediaPlayerUltimate.Elements;
using CustomMediaPlayerUltimate.DataStructures;

namespace CustomMediaPlayerUltimate;

public partial class MainWindow : Window
{
    public static readonly string MUSIC_PATH = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic);
    public static readonly string PLAYLISTS_PATH = $"{MUSIC_PATH}\\HompPlaylists";
    public static readonly string LYRICS_PATH = $"{MUSIC_PATH}\\Lyrics";
    public static readonly string COVERS_PATH = $"{MUSIC_PATH}\\HompCovers";
    public const string UNKNOWN_ALBUM = "Unknown Album";
    public const string TIME_FORMAT = "m':'ss";
    public const string TOTAL_TIME_FORMAT = "m'min 'ss's'";
    public const string TOTAL_TIME_FORMAT_HOURS = $"h'h '{TOTAL_TIME_FORMAT}";
    private const int VOLUME_STEP = 2;
    private static readonly string[] ALLOWED_EXTENSIONS = { ".mp3", ".flac" };
    private static bool dbExists = false;
    public static MainWindow Instance = null!;

    private static readonly GridLength collapsedLyricsTextBoxWidth = new GridLength(0f);
    private static readonly GridLength expandedLyricsTextBoxWidth = new GridLength(400f);

    private WindowState lastWindowState = WindowState.Normal;
    private bool isSidebarVisible = true;
    private (bool SaveValue, bool Val) IsSidebarVisible
    {
        get { return (false, isSidebarVisible); }
        set
        {
            isSidebarVisible = value.Val;
            SongLyricsRichTextBoxColumn.Width = value.Val ? expandedLyricsTextBoxWidth : collapsedLyricsTextBoxWidth;
            SongLyricsRichTextBox.Visibility = value.Val ? Visibility.Visible : Visibility.Collapsed;
            SongLyricsRichTextBoxVisibilityButton.IsChecked = value.Val;
            if (value.SaveValue) lastIsSidebarVisible = value.Val;
        }
    }
    private bool lastIsSidebarVisible = true;

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
    private CustomSongElement.CustomSongElementInfo? currentSongElementInfo = null;
    private ListView? currentSongElementInfoListView = null;
    private PlaylistElement? currentlySelectedPlaylistElement;
    private CollectionElement? currentlySelectedAlbumElement;
    private CollectionElement? currentlySelectedArtistElement;

    public List<CustomSongElement.CustomSongElementInfo>? AllSongs { get; set; }
    public ObservableCollection<CustomSongElement.CustomSongElementInfo> PlaylistSongs { get; set; } = new();
    public ObservableCollection<CustomSongElement.CustomSongElementInfo> AlbumSongs { get; set; } = new();
    public ObservableCollection<CustomSongElement.CustomSongElementInfo> ArtistSongs { get; set; } = new();
    public ObservableCollection<CustomSongElement.CustomSongElementInfo> QueueSongs { get; set; } = new();
    public ObservableCollection<CustomSongElement.CustomSongElementInfo> SearchSongs { get; set; } = new();
    private SongCollection? currentCollection;
    private SongCollection? allSongsPlaylist;
    private Dictionary<string, SongCollection> playlists = new Dictionary<string, SongCollection>();
    private Dictionary<string, SongCollection> albums = new Dictionary<string, SongCollection>();
    private Dictionary<string, SongCollection> artists = new Dictionary<string, SongCollection>();

    public MainWindow()
    {
        InitializeComponent();

        Logger.Init();
        Logger.Log("HOMP started.");

        Instance = this;
        DataContext = this;
#if DEBUG
        Title = $"(DEBUG) {Title}";
        Logger.Warn("Running HOMP in Debug mode.");
#endif

        KeyboardHook.OnKeyPressed += HandleHotkey;
        KeyboardHook.Start();
    }

    public async void OnWindowLoaded(object sender, RoutedEventArgs e)
    {
        EnsureFolders();
        dbExists = File.Exists(Database.DB_PATH);
        Logger.Log($"Database file found: {dbExists}");
        if (!Database.Init(dbExists))
        {
            MessageBox.Show("Database initialization failed. Will quit.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
        }

        mediaPlayer.MediaOpened += MediaPlayer_MediaOpened;
        mediaPlayer.MediaEnded += MediaPlayer_MediaEnded;
        mediaPlayer.MediaFailed += MediaPlayer_MediaFailed;

        timer.Interval = TimeSpan.FromMilliseconds(100);
        timer.Tick += Timer_Tick;
        timer.Start();

        LoadSettings();

        if (!dbExists)
        {
            UpdateLoadingLabelsToSave();
            await Task.Run(SaveAllToDB);
        }
        await LoadAllFromDB();

        LoadAllAlbums();
        LoadAllArtists();

        FinishedLoading();
    }

    private void EnsureFolders()
    {
        Directory.CreateDirectory(PLAYLISTS_PATH);
        Directory.CreateDirectory(LYRICS_PATH);
        Directory.CreateDirectory(COVERS_PATH);
        Logger.Log("HOMP folders created.");
    }

    private void OnWindowClosing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        Properties.Settings.Default.Save();
        KeyboardHook.Stop();
        Database.Close();
    }

    private void OnWindowSizeChanged(object sender, SizeChangedEventArgs e)
    {
        bool mustAdapt = ActualWidth <= 600;
        IsSidebarVisible = mustAdapt ? (false, false) : (false, lastIsSidebarVisible);
    }

    private void OnWindowStateChanged(object sender, EventArgs e)
    {
        if (WindowState != WindowState.Minimized)
        {
            lastWindowState = WindowState;
            return;
        }
        if (Properties.Settings.Default.MiniplayerAppearOnMinimize)
        {
            ShowMiniplayer();
        }
    }

    private void OnWindowKeyUp(object sender, KeyEventArgs e)
    {
        if (Keyboard.FocusedElement != this) return;
        if (e.Key == Key.Space)
        {
            TogglePlayPause();
        }
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
            else if (key == (Key)Properties.Settings.Default.GoToBeginningShortcutKey)
            {
                GoToBeginningOfSong();
            }
            else if (key == (Key)Properties.Settings.Default.SwitchMiniplayerShortcutKey)
            {
                SwitchMiniplayerView();
            }
        }
        else
        {
            if (key == Key.MediaPreviousTrack)
            {
                PreviousSongInPlaylist();
            }
            else if (key == Key.MediaPlayPause)
            {
                TogglePlayPause();
            }
            else if (key == Key.MediaNextTrack)
            {
                NextSongInPlaylist();
            }
        }
    }

    private void HideAllViews()
    {
        AlbumsView.Visibility = Visibility.Collapsed;
        ArtistsView.Visibility = Visibility.Collapsed;
        PlaylistsView.Visibility = Visibility.Collapsed;
        QueueView.Visibility = Visibility.Collapsed;
        SearchResultsView.Visibility = Visibility.Collapsed;
        SettingsView.Visibility = Visibility.Collapsed;
        AllSongsView.Visibility = Visibility.Collapsed;

        AllSongsTabButton.Tag = null;
        PlaylistsTabButton.Tag = null;
        AlbumsTabButton.Tag = null;
        ArtistsTabButton.Tag = null;
        QueueTabButton.Tag = null;
        SearchResultsTabButton.Tag = null;
        SettingsTabButton.Tag = null;
    }

    private void SwitchToAllSongsView(object sender, RoutedEventArgs e)
    {
        HideAllViews();
        AllSongsView.Visibility = Visibility.Visible;
        AllSongsTabButton.Tag = "Focused";
    }

    private void SwitchToPlaylistsView(object sender, RoutedEventArgs e)
    {
        HideAllViews();
        PlaylistsView.Visibility = Visibility.Visible;
        PlaylistsTabButton.Tag = "Focused";
    }

    private void SwitchToAlbumsView(object sender, RoutedEventArgs e)
    {
        HideAllViews();
        AlbumsView.Visibility = Visibility.Visible;
        AlbumsTabButton.Tag = "Focused";
    }

    private void SwitchToArtistsView(object sender, RoutedEventArgs e)
    {
        HideAllViews();
        ArtistsView.Visibility = Visibility.Visible;
        ArtistsTabButton.Tag = "Focused";
    }

    private void SwitchToQueueView(object sender, RoutedEventArgs e)
    {
        HideAllViews();
        QueueView.Visibility = Visibility.Visible;
        QueueTabButton.Tag = "Focused";
    }

    private void SwitchToSearchResultsView(object sender, RoutedEventArgs e)
    {
        HideAllViews();
        SearchResultsView.Visibility = Visibility.Visible;
        SearchResultsTabButton.Tag = "Focused";
    }

    private void SwitchToSettingsView(object sender, RoutedEventArgs e)
    {
        HideAllViews();
        SettingsView.Visibility = Visibility.Visible;
        SettingsTabButton.Tag = "Focused";
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
            else
            {
                mediaPlayer.Play();
            }
            IsPlaying = true;
        }
    }

    private void ProgressSliderMouseMove(object sender, RoutedEventArgs e)
    {
        if (!isProgressSliderBeingDragged) return;
        mediaPlayer.Position = TimeSpan.FromMilliseconds(ProgressSlider.Value);
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
        Properties.Settings.Default.Save();
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
        Properties.Settings.Default.Save();
    }

    private void ShuffleToggleButtonClick(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.PlayerShuffle = (bool)ShuffleToggleButton.IsChecked!;
        Properties.Settings.Default.Save();
    }

    private void BackwardButtonClick(object sender, RoutedEventArgs e)
    {
        PreviousSongInPlaylist();
    }

    private void ForwardButtonClick(object sender, RoutedEventArgs e)
    {
        NextSongInPlaylist();
    }

    public void PlaylistElementClick(PlaylistElement element)
    {
        if (currentlySelectedPlaylistElement == element) return;
        if (currentlySelectedPlaylistElement is not null)
            currentlySelectedPlaylistElement.Focused = false;
        LoadPlaylistSongsInView(playlists[element.Text]);
        currentlySelectedPlaylistElement = element;
    }

    public void PlaylistElementDoubleClick(PlaylistElement element)
    {
        PlayCollection(element.Playlist);
    }

    public void PlaylistElementManageClick(PlaylistElement element)
    {
        ManagePlaylistSongs(element.Playlist);
    }

    public void PlaylistElementRenameClick(PlaylistElement element)
    {
        RenamePlaylist(element.Playlist);
    }

    public void PlaylistElementDeleteClick(PlaylistElement element)
    {
        DeletePlaylist(element.Playlist);
    }

    private void NewPlaylistButtonClick(object sender, RoutedEventArgs e)
    {
        Logger.Log("Creating new playlist...");
        InputBox ib = new InputBox("Insert playlist name", "Type the name you want to give to the playlist:");
        if (ib.ShowDialog() != true)
        {
            Logger.Log("New playlist creation canceled.");
            return;
        }

        string name = ib.InputTextBox.Text.Trim();
        Logger.Log($"New playlist name: '{name}'.");
        if (string.IsNullOrEmpty(name))
        {
            Logger.Log("Playlist creation aborted: chosen name is null or empty.");
            MessageBox.Show($"The name you chose is not valid. Choose a valid name.");
            return;
        }
        if (Database.DoesPlaylistExist(name) ?? true || playlists.ContainsKey(name))
        {
            Logger.Error($"Cannot create playlist! A playlist with the name '{name}' already exists.");
            MessageBox.Show($"Cannot create playlist. A playlist with the name '{name}' already exists.");
            return;
        }

        if (!Database.AddPlaylist(name))
        {
            Logger.Error("Cannot create playlist! Failed to save data in table 'playlists'.");
            MessageBox.Show("An error occurred while creating the playlist.");
            return;
        }
        Logger.Log("Playlist saved to database.");

        // We can add the new playlist to the playlists list.
        // We are sure it exists in the DB now.
        playlists.Add(name, new SongCollection(name, SongCollectionType.Playlist));
        PlaylistsListPanel.Children.Add(new PlaylistElement(playlists[name]) { Text = name });
        Logger.Log("Playlist added to UI.");
    }

    private void RenamePlaylist(SongCollection playlist)
    {
        string oldName = playlist.Name;
        bool wasPlaying = currentCollection == playlist;
        Logger.Log($"Renaming playlist '{oldName}'...");
        if (playlist.Equals(SongCollection.EmptyPlaylist))
        {
            Logger.Warn($"Skipping playlist renaming: playlist is SongCollection.Empty!");
            return;
        }
        InputBox ib = new InputBox("Insert playlist name", "Type the new name you want to give to the playlist:");
        if (ib.ShowDialog() != true)
        {
            Logger.Log("Playlist renaming canceled.");
            return;
        }

        string newName = ib.InputTextBox.Text.Trim();
        if (string.IsNullOrEmpty(newName))
        {
            Logger.Log("Playlist renaming aborted: chosen name is null or empty.");
            MessageBox.Show($"The name you chose is not valid. Choose a valid name.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (oldName == newName)
        {
            Logger.Log("Playlist renaming aborted: new name is the same as the old one.");
            MessageBox.Show("The name you chose is the same as the current one. Choose a different name.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (Database.DoesPlaylistExist(newName) ?? true || playlists.ContainsKey(newName))
        {
            Logger.Error($"Cannot rename playlist! A playlist with the name '{newName}' already exists.");
            MessageBox.Show($"Cannot rename playlist. A playlist with the name '{newName}' already exists.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        int renameResult = Database.ExecuteGenericQuery("UPDATE playlists SET name = @_newName WHERE name = @_oldName;",
            [("@_newName", newName), ("@_oldName", oldName)]);
        if (renameResult == -2)
        {
            Logger.Error("Cannot rename playlist! Failed to update 'name' in table 'playlists'.");
            MessageBox.Show("An error occurred while renaming the playlist.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        // We update the playlist's name here because we are sure
        // that the name in the DB has actually been changed.
        playlist.Name = newName;

        // Maybe do the inverse?
        playlists.Remove(oldName);
        playlists.Add(newName, playlist);
        if (wasPlaying)
        {
            ResetPlayback();
        }

        PlaylistElement? oldPlaylistElement = PlaylistsListPanel.Children
            .OfType<PlaylistElement>()
            .Where(el => el.Text == oldName)
            .FirstOrDefault();
        if (oldPlaylistElement == default(PlaylistElement))
        {
            Logger.Warn("Playlist to be renamed is not the same as the currently focused one.");
        }
        else
        {
            bool wasElementFocused = oldPlaylistElement.Focused;
            PlaylistsListPanel.Children.Remove(oldPlaylistElement);
            PlaylistElement newElement = new PlaylistElement(playlists[newName]) { Text = newName };
            PlaylistsListPanel.Children.Add(newElement);
            if (wasElementFocused)
            {
                PlaylistElementClick(newElement);
                newElement.Focused = true;
            }
        }
        Logger.Log("Playlist renamed.");
    }

    private void DeletePlaylist(SongCollection playlist)
    {
        Logger.Log($"Deleting playlist with name '{playlist.Name}'...");
        if (playlist.Equals(SongCollection.EmptyPlaylist))
        {
            Logger.Warn("Skipping playlist deletion: playlist is SongCollection.Empty!");
            return;
        }
        if (MessageBox.Show($"Are you sure to delete the playlist \"{playlist.Name}\"?", "Confirmation", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.No)
        {
            Logger.Log("Playlist deletion canceled.");
            return;
        }

        using SqliteDataReader? r = Database.ExecuteSelectQuery("SELECT id FROM playlists WHERE name = @_name;",
            [("@_name", playlist.Name)]);
        if (r is null)
        {
            Logger.Error("Cannot delete playlist! Failed to obtain data from 'playlists' table.");
            MessageBox.Show("An error occurred while deleting the playlist.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (r.IsClosed)
        {
            Logger.Error("Cannot delete playlist! Database reader is closed.");
            MessageBox.Show("An error occurred while deleting the playlist.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        int playlistId = -1;
        if (!r.Read())
        {
            Logger.Error("Cannot delete playlist! Failed to read playlist id from database.");
            MessageBox.Show("An error occurred while deleting the playlist.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        playlistId = r.GetInt32(0);

        if (!Database.StartTransaction())
        {
            Logger.Log("Playlist deletion aborted: cannot start database transaction.");
            MessageBox.Show("An error occurred while deleting the playlist.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        int result = Database.ExecuteGenericQuery("DELETE FROM playlist_songs WHERE playlist_id = @_id;",
            [("@_id", playlistId)]);
        if (result == -2)
        {
            Logger.Error("Cannot delete playlist! Failed to delete data from 'playlist_songs' table.");
            MessageBox.Show("An error occurred while deleting the playlist.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            Database.CancelTransaction();
            return;
        }
        result = Database.ExecuteGenericQuery("DELETE FROM playlists WHERE id = @_id", [("@_id", playlistId)]);
        if (result == -2)
        {
            Logger.Error("Cannot delete playlist! Failed to delete data from 'playlists' table.");
            MessageBox.Show("An error occurred while deleting the playlist.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            Database.CancelTransaction();
            return;
        }
        if (!Database.EndTransaction())
        {
            Logger.Log("Playlist deletion failed: cannot commit database transaction.");
            MessageBox.Show("An error occurred while deleting the playlist.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (currentCollection == playlist)
        {
            ResetPlayback();
        }
        playlists.Remove(playlist.Name);
        PlaylistElement? playlistElement = PlaylistsListPanel.Children
            .OfType<PlaylistElement>()
            .Where(el => el.Text == playlist.Name)
            .FirstOrDefault();
        if (currentlySelectedPlaylistElement == playlistElement)
        {
            PlaylistsListPanel.Children.Remove(playlistElement);
            ClearPlaylistSongsView();
        }
        Logger.Log("Playlist deleted.");
    }

    private void ManagePlaylistSongs(SongCollection playlist)
    {
        Logger.Log($"Managing songs in playlist with name '{playlist.Name}'...");
        if (playlist.Equals(SongCollection.EmptyPlaylist))
        {
            Logger.Warn("Skipping playlist management: playlist is SongCollection.Empty!");
            return;
        }
        SongsChooserDialog scd = new SongsChooserDialog(allSongsPlaylist!, playlist);
        if (scd.ShowDialog() != true)
        {
            Logger.Log("Playlist management canceled.");
            return;
        }

        List<Song> result = scd.Result;
        using SqliteDataReader? r = Database.ExecuteSelectQuery("SELECT id FROM playlists WHERE name = @_name;",
            [("@_name", playlist.Name)]);
        if (r is null)
        {
            Logger.Error("Cannot manage playlist! Failed to obtain data from 'playlists' table.");
            MessageBox.Show("An error occurred.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (r.IsClosed)
        {
            Logger.Error("Cannot manage playlist! Database reader is closed.");
            MessageBox.Show("An error occurred.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        int playlistId = -1;
        if (!r.Read())
        {
            Logger.Error("Cannot manage playlist! Failed to read playlist id from database.");
            MessageBox.Show("An error occurred.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        playlistId = r.GetInt32(0);

        int status = Database.ExecuteGenericQuery("DELETE FROM playlist_songs WHERE playlist_id = @_id;",
            [("@_id", playlistId)]);
        if (status == -2)
        {
            Logger.Error("Cannot manage playlist! Failed to delete data from 'playlist_songs' table.");
            MessageBox.Show("An error occurred while saving changes to the playlist.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        if (!Database.StartTransaction())
        {
            Logger.Log("Playlist management aborted: cannot start database transaction.");
            MessageBox.Show("An error occurred while saving changes to the playlist.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        for (int i = 0; i < result.Count; i++)
        {
            if (!Database.AddSongToPlaylist(result[i].FilePath, playlistId))
            {
                Logger.Warn($"Playlist management: skipped song '{result[i].FilePath}' in playlist {playlistId}.");
            }
        }
        if (!Database.EndTransaction())
        {
            Logger.Log("Playlist management failed: cannot commit database transaction.");
            MessageBox.Show("An error occurred while saving changes to the playlist.", "HOMP", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        playlists[playlist.Name].Songs.Clear();
        for (int i = 0; i < result.Count; i++)
        {
            playlists[playlist.Name].AddSong(result[i]);
        }

        PlaylistElement? playlistElement = PlaylistsListPanel.Children
            .OfType<PlaylistElement>()
            .Where(el => el.Text == playlist.Name)
            .FirstOrDefault();
        if (currentlySelectedPlaylistElement == playlistElement)
        {
            LoadPlaylistSongsInView(playlists[playlist.Name]);
        }
        Logger.Log("Finished playlist management.");
    }

    public void LoadLyricsInView()
    {
        if (currentSong is null) return;
        string lyricsFileName = $"{LYRICS_PATH}\\{currentSong.FileName}.mp3[Lyrics].txt";
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

    private void ClearPlaylistSongsView()
    {
        PlaylistSongs.Clear();
        PlaylistSongsListCoverImage.Source = null;
        PlaylistSongsListTitleLabel.Content = string.Empty;
        PlaylistSongsListDurationLabel.Content = string.Empty;
        currentlySelectedPlaylistElement = null;
    }

    private void LoadPlaylistSongsInView(SongCollection playlist)
    {
        if (playlist.Equals(SongCollection.EmptyPlaylist)) return;
        PlaylistSongs.Clear();
        PlaylistSongsListTitleLabel.Content = playlist.Name;
        TimeSpan totalTime = playlist.TotalTime;
        PlaylistSongsListDurationLabel.Content =
            $"{playlist.Songs.Count} songs - {totalTime.ToString(((int)totalTime.TotalHours > 0) ? TOTAL_TIME_FORMAT_HOURS : TOTAL_TIME_FORMAT)}";
        foreach (KeyValuePair<string, Song> songPair in playlists[playlist.Name].Songs.OrderBy((kvp) => kvp.Key))
        {
            PlaylistSongs.Add(new(songPair.Value, playlists[playlist.Name]));
        }
    }

    private void LoadAlbumSongsInView(string albumName)
    {
        if (!albums.ContainsKey(albumName)) { MessageBox.Show("Could not load album."); return; }
        AlbumSongs.Clear();
        AlbumSongsListTitleLabel.Content = albumName;
        TimeSpan totalTime = albums[albumName].TotalTime;
        AlbumSongsListDurationLabel.Content =
            $"{albums[albumName].Songs.Count} songs - {totalTime.ToString(((int)totalTime.TotalHours > 0) ? TOTAL_TIME_FORMAT_HOURS : TOTAL_TIME_FORMAT)}";
        foreach (Song song in albums[albumName].Songs.Values)
        {
            AlbumSongs.Add(new(song, albums[albumName]));
        }
    }

    private void LoadArtistSongsInView(string artistName)
    {
        if (!artists.ContainsKey(artistName)) { MessageBox.Show("Could not load artist's songs."); return; }
        ArtistSongs.Clear();
        ArtistSongsListTitleLabel.Content = artistName;
        TimeSpan totalTime = artists[artistName].TotalTime;
        ArtistSongsListDurationLabel.Content =
            $"{artists[artistName].Songs.Count} songs - {totalTime.ToString(((int)totalTime.TotalHours > 0) ? TOTAL_TIME_FORMAT_HOURS : TOTAL_TIME_FORMAT)}";
        foreach (Song song in artists[artistName].Songs.Values)
        {
            ArtistSongs.Add(new(song, artists[artistName]));
        }
    }

    private void SearchInputTextBoxKeyUp(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            IEnumerable<Song> results = from song in allSongsPlaylist!.Songs.Values
                                        where song.IsCorrelated(SearchInputTextBox.Text)
                                        select song;
            SongCollection playlist = allSongsPlaylist;
            if (Properties.Settings.Default.UseSearchResultsAsShuffleSource)
            {
                playlist = new SongCollection("__HOMP_SEARCH_RESULTS_PLAYLIST__", SongCollectionType.Playlist);
                foreach (Song song in results)
                {
                    playlist.AddSong(song);
                }
            }
            SearchSongs.Clear();
            foreach (Song song in results)
            {
                SearchSongs.Add(new(song, playlist));
            }
            SearchResultsTitleLabel.Content = $"Search results for '{SearchInputTextBox.Text}'";
            TimeSpan totalTime = TimeSpan.FromTicks(SearchSongs.Sum(song => song.Song.Duration.Ticks));
            SearchResultsSubtitleLabel.Content =
                $"{Utils.Pluralize(SearchSongs.Count, "song", "songs")} - {
                    totalTime.ToString(((int)totalTime.TotalHours > 0) ? TOTAL_TIME_FORMAT_HOURS : TOTAL_TIME_FORMAT)}";
            SwitchToSearchResultsView(null!, null!);
        }
    }

    private void SaveAllToDB()
    {
        Logger.Log("Caching all files to the database...");
        if (!Database.StartTransaction())
        {
            Logger.Error("Caching files failed! Database transaction denied.");
            return;
        }
        foreach (string? path in Properties.Settings.Default.SourceDirectories)
        {
            if (path is null) continue;
            if (!Directory.Exists(path)) continue;
            // Save source directory
            Logger.Log($"Caching folder '{path}'.");
            Database.AddFolder(path);
            int songsCached = 0;

            var songPaths = Directory.EnumerateFiles(path, "*", SearchOption.TopDirectoryOnly);
            foreach (string songPath in songPaths)
            {
                if (!ALLOWED_EXTENSIONS.Contains(new FileInfo(songPath).Extension)) continue;
                Song song = new(songPath);
                var mediaState = Utils.GetMediaInformation(song);
                if (!mediaState.Success)
                {
                    Logger.Exception($"Skipped {songPath}: error while trying to retrieve media information", mediaState.Exception!);
                    continue;
                }
                if (Database.AddSong(song))
                {
                    songsCached++;
                }
                else
                {
                    Logger.Error($"Caching of song {songPath} failed!");
                }
            }
            Logger.Log($"Finished caching folder '{path}', cached {songsCached} song files.");
        }
        Logger.Log("Finished caching all folders.");
        Database.SetLastUpdate(DateTime.Now.Ticks);
        if (!Database.EndTransaction())
        {
            Logger.Error("Caching files failed! Database transaction cannot be committed.");
        }
    }

    private async Task LoadAllFromDB()
    {
        // We get all the source directories used in the DB.
        // TODO: check if the dirs in the DB are in the Settings.SourceDirectories
        // setting, otherwise, delete it from DB and save that directory instead.
        Logger.Log("Loading all files from database...");
        List<string?> dirs = new(Properties.Settings.Default.SourceDirectories.Count);
        var dirReader = Database.ExecuteSelectQuery("SELECT path FROM folders;");
        if (dirReader is null)
        {
            Logger.Error("Cannot load files from database! Failed to obtain data from 'folders' table.");
            return;
        }
        if (dirReader.IsClosed)
        {
            Logger.Error("Cannot load files from database! Database reader is closed.");
            return;
        }
        while (dirReader.Read())
        {
            dirs.Add(dirReader.GetString(0));
        }
        Logger.Log($"Loaded {dirs.Count} directories from database.");
        dirReader.Close();

        // If the DB did not exist when opening HOMP, this is the first time
        // we are loading songs from it, and it just got created, so we can
        // skip all the update checks.
        // If it existed, then we need to do these checks.
        if (dbExists)
        {
            // Get the last time we updated the DB.
            var updReader = Database.ExecuteSelectQuery("SELECT last_db_update FROM homp;");
            if (updReader is null)
            {
                Logger.Log("Cannot load files from database! Failed to obtain data from 'homp' table.");
                return;
            }
            if (!updReader.Read())
            {
                Logger.Log("Cannot load files from database! Failed to retrieve 'last_db_update'.");
                return;
            }
            long lastUpdate = updReader.GetInt64(0);
            Logger.Log($"Retrieved 'last_db_update' with value '{lastUpdate}'.");
            updReader.Close();

            // We get the files of each dir and check if there are new ones.
            for (int i = 0; i < dirs.Count; i++)
            {
                if (dirs[i] is null)
                {
                    Logger.Warn($"Skipping new files check from directory '{dirs[i]}' (from database): directory is null!");
                    continue;
                }
                FileInfo[] newFiles = new DirectoryInfo(dirs[i]!)
                    .GetFiles()
                    .Where(i => i.LastWriteTime.Ticks > lastUpdate || i.CreationTime.Ticks > lastUpdate)
                    .Where(i => ALLOWED_EXTENSIONS.Contains(i.Extension))
                    .ToArray();
                Logger.Log($"Obtained {newFiles.Length} new/modified files from '{dirs[i]}'.");
                // If there are any new files, we read them back from
                // disk and save them to the DB.
                if (newFiles.Length != 0)
                {
                    Logger.Log("Saving them to database...");
                    // Read each new file from disk
                    // and save it to DB.
                    int cachedFiles = 0;
                    int updatedFiles = 0;
                    for (int j = 0; j < newFiles.Length; j++)
                    {
                        Song song = new(newFiles[j].FullName);
                        var mediaState = Utils.GetMediaInformation(song);
                        if (!mediaState.Success)
                        {
                            Logger.Exception($"Skipped new/modified file '{newFiles[j].FullName}': error while trying to retrieve media information", mediaState.Exception!);
                            continue;
                        }

                        if (Database.DoesSongExist(song.FilePath) ?? true)
                        {
                            if (!Database.UpdateSong(song))
                            {
                                Logger.Log($"Failed to update new/modified file '{newFiles[j].FullName}'.");
                                continue;
                            }
                            updatedFiles++;
                        }
                        else
                        {
                            if (!Database.AddSong(song))
                            {
                                Logger.Log($"Failed to cache new/modified file '{newFiles[j].FullName}'.");
                                continue;
                            }
                            cachedFiles++;
                        }
                    }
                    Database.SetLastUpdate(DateTime.Now.Ticks);
                    Logger.Log($"Successfully cached {cachedFiles} file(s) and updated {updatedFiles} file(s).");
                }
            }
        }

        // We initialize the necessary collections.
        int songCount = Database.GetRowCount("songs");
        Logger.Log($"Got {songCount} songs from database. Initializing songs collections.");
        AllSongs = new(songCount <= 0 ? 32 : songCount);
        allSongsPlaylist = new SongCollection("__HOMP_ALL_SONGS_PLAYLIST__", SongCollectionType.Playlist);

        // Songs
        Logger.Log("Loading all songs from database...");
        for (int i = 0; i < dirs.Count; i++)
        {
            if (dirs[i] is null)
            {
                Logger.Warn($"Skipping songs from directory '{dirs[i]}' (from database): directory is null!");
                continue;
            }
            using (var reader = Database.ExecuteSelectQuery("SELECT * FROM songs WHERE path LIKE @_path;", [("@_path", $"{dirs[i]}\\%")]))
            {
                if (reader is null)
                {
                    Logger.Error($"Cannot load songs from database! Failed to obtain data from 'songs' table with directory '{dirs[i]}'.");
                    continue;
                }
                while (reader!.Read())
                {
                    string? songPath = reader!.GetString(0);
                    await LoadSongFromDB(dirs[i]!, songPath, reader);
                }
            }
        }
        AllSongs.Sort((a, b) =>
        {
            if (a is null || b is null) return 0;
            // Get sort based on artist name
            int fs = a.Song.Artist.CompareTo(b.Song.Artist);
            // If same artist, return comparison based on title
            if (fs == 0) return a.Song.Title.CompareTo(b.Song.Title);
            // Else return artist sort
            return fs;
        });
        Logger.Log("Finished loading all songs from database.");

        // Playlists
        Logger.Log("Loading all playlists from database...");
        List<(int Id, string? Name)> dbPlaylists = new(8); // Arbitrary number, seems fine
        using (var playlistsReader = Database.ExecuteSelectQuery("SELECT * FROM playlists;"))
        {
            if (playlistsReader is not null)
            {
                while (playlistsReader.Read())
                {
                    dbPlaylists.Add(new(playlistsReader.GetInt32(0), playlistsReader.GetString(1)));
                }
            }
            else
            {
                Logger.Error("Cannot load playlists from database! Failed to obtain data from 'playlists' table.");
            }
        }
        for (int i = 0; i < dbPlaylists.Count; i++)
        {
            if (dbPlaylists[i].Name is null)
            {
                Logger.Error($"Skipping playlist '{dbPlaylists[i].Name}' (from database): playlist name is null!");
                continue;
            }
            using (SqliteDataReader? plReader =
                Database.ExecuteSelectQuery("SELECT song_path FROM playlist_songs WHERE playlist_id = @_id",
                [("@_id", dbPlaylists[i].Id)]))
            {
                if (plReader is null)
                {
                    Logger.Error($"Cannot load playlist {dbPlaylists[i].Name} from database! Failed to obtain data from 'playlist_songs' table.");
                    continue;
                }
                SongCollection playlist = new(dbPlaylists[i].Name!, SongCollectionType.Playlist);
                while (plReader!.Read())
                {
                    string songPath = plReader!.GetString(0);
                    if (allSongsPlaylist.Songs.ContainsKey(songPath))
                    {
                        playlist.AddSong(allSongsPlaylist.Songs[songPath]);
                    }
                }
                playlists.Add(playlist.Name, playlist);
                PlaylistsListPanel.Children.Add(new PlaylistElement(playlist) { Text = playlist.Name });
            }
        }
        Logger.Log($"Finished loading all playlists from database.");

        Logger.Log($"Finished loading all files from database.");
        Logger.Log("Now showing all songs...");
        AllSongsListView.ItemsSource = new ObservableCollection<CustomSongElement.CustomSongElementInfo>(AllSongs);
        Logger.Log("Songs are now shown in the UI.");
    }

    // No logs present because this is a critical function.
    private async Task LoadSongFromDB(string dir, string? songPath, SqliteDataReader reader)
    {
        Song song = new(songPath!)
        {
            FileName = songPath?.Replace($"{dir}\\", "").Replace(".mp3", "") ?? string.Empty,
            Title = reader.GetString(1) ?? string.Empty,
            Artist = reader.GetString(2) ?? string.Empty,
            // AlbumArtist = reader?.GetString(3) ?? "",
            Album = reader.GetString(4) ?? UNKNOWN_ALBUM,
            Year = reader.GetInt32(5),
            TrackNumber = reader.GetInt32(6),
            Duration = TimeSpan.FromTicks(reader.GetInt64(8)),
            Rating = reader.GetByte(9),
        };

        song.Artists = song.Artist.Split(',', StringSplitOptions.TrimEntries);
        song.Genres =
            reader.GetString(7)?.Split(',', StringSplitOptions.TrimEntries)
            ?? Array.Empty<string>();

        // Could maybe be a setting with values [Individual, Combined, Both]?
        // Single artists
        foreach (string artist in song.Artists)
        {
            if (!artists.ContainsKey(artist))
                artists[artist] = new SongCollection(artist, SongCollectionType.Artist);
            artists[artist].AddSong(song);
        }
        // Combined artists
        if (!artists.ContainsKey(song.Artist))
            artists[song.Artist] = new SongCollection(song.Artist, SongCollectionType.Artist);

        // If the song has a single artist, this would have caused problems.
        // So we check before adding it.
        if (!artists[song.Artist].Songs.ContainsKey(song.FilePath))
            artists[song.Artist].AddSong(song);

        // Create the album if it does not exist yet.
        if (!albums.ContainsKey(song.Album ?? UNKNOWN_ALBUM))
            albums[song.Album ?? UNKNOWN_ALBUM] = new SongCollection(song.Album ?? UNKNOWN_ALBUM, SongCollectionType.Album);

        // TODO: Slow. Maybe convert to ID3 cover tag?
        //if (File.Exists($"{COVERS_PATH}\\{song.FileName}.mp3[Cover].png"))
        //{
        //    song.Cover = Utils.ConstructImageFromPath($"{COVERS_PATH}\\{song.FileName}.mp3[Cover].png", UriKind.Absolute);
        //}

        allSongsPlaylist?.AddSong(song);
        albums[song.Album ?? UNKNOWN_ALBUM].AddSong(song);

        await Task.Run(() =>
        {
            AllSongs?.Add(new(song, allSongsPlaylist!));
        });
    }

    private void LoadAllAlbums()
    {
        Logger.Log("Loading all albums...");
        Logger.Log($"Albums found: {albums.Count}.");
        AlbumsListPanel.Children.Clear();
        foreach (KeyValuePair<string, SongCollection> kvp in albums.OrderBy((kvp) => kvp.Key))
        {
            AlbumsListPanel.Children.Add(new CollectionElement(
            (self) =>
            {
                if (currentlySelectedAlbumElement == self) return;
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
        Logger.Log("Albums loaded.");
    }

    private void LoadAllArtists()
    {
        Logger.Log("Loading all artists...");
        Logger.Log($"Artists found: {artists.Count}.");
        ArtistsListPanel.Children.Clear();
        foreach (KeyValuePair<string, SongCollection> kvp in artists.OrderBy((kvp) => kvp.Key))
        {
            ArtistsListPanel.Children.Add(new CollectionElement(
            (self) =>
            {
                if (currentlySelectedArtistElement == self) return;
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
        Logger.Log("Artists loaded.");
    }

    private void LoadSettings()
    {
        Logger.Log("Loading settings...");
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
        SettingsGoToBeginningOfSongShortcutButton.Content = $"{(Key)Properties.Settings.Default.GoToBeginningShortcutKey}";

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
        SettingsMiniplayerAutoAppearOnMinimizeCheckbox.IsChecked = Properties.Settings.Default.MiniplayerAppearOnMinimize;
        SettingsMiniplayerHideControlsCheckbox.IsChecked = Properties.Settings.Default.MiniplayerHideControls;
        Logger.Log("Settings loaded.");
    }

    private void UpdateLoadingLabelsToSave()
    {
        Logger.Log("Changing loading labels to save labels.");
        AllSongsViewLoadingOverlayLabel.Content = "Loading and caching songs...";
        PlaylistsViewLoadingOverlayLabel.Content = "Waiting for songs to load...";
        AlbumsViewLoadingOverlayLabel.Content = "Waiting for songs to load...";
        ArtistsViewLoadingOverlayLabel.Content = "Waiting for songs to load...";
    }

    private void FinishedLoading()
    {
        Logger.Log("Finished loading everything.");
        SearchInputTextBox.IsEnabled = true;
        NewPlaylistButton.IsEnabled = true;

        AllSongsViewLoadingOverlay.Visibility = Visibility.Collapsed;
        PlaylistsViewLoadingOverlay.Visibility = Visibility.Collapsed;
        AlbumsViewLoadingOverlay.Visibility = Visibility.Collapsed;
        ArtistsViewLoadingOverlay.Visibility = Visibility.Collapsed;
        QueueViewLoadingOverlay.Visibility = Visibility.Collapsed;

        AllSongsViewContent.Visibility = Visibility.Visible;
        PlaylistsViewContent.Visibility = Visibility.Visible;
        AlbumsViewContent.Visibility = Visibility.Visible;
        ArtistsViewContent.Visibility = Visibility.Visible;
        QueueViewContent.Visibility = Visibility.Visible;
    }

    private void Timer_Tick(object? sender, EventArgs e)
    {
        if (mediaPlayer.Source == null) return;
        if (!mediaAvailable) return;
        if (!isProgressSliderBeingDragged)
        {
            ProgressSlider.Value = mediaPlayer.Position.TotalMilliseconds;
            MiniPlayerWindow.Instance?.SetProgress(ProgressSlider.Value);
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

        Song song = allSongsPlaylist!.Songs[songFile];
        UpdatePlayingSongElement(song, songCollection);

        string title = (song.Title == string.Empty) ? "Generic Song" : song.Title;
        string artist = song.Artist;

        MiniPlayerWindow.Instance?.SetTitleText(title);
        MiniPlayerWindow.Instance?.SetArtistText(artist);
        MiniPlayerWindow.Instance?.SetCover(song.Cover);

        if (song.Album is not null)
        {
            artist += " - " + song.Album;
        }
        CurrentSongTitleLabel.Content = title;
        CurrentSongArtistAlbumLabel.Content = artist;
        QueueCurrentSongCSE.SetSongInfo(new(song, songCollection) { IsPlaying = true });

        mediaPlayer.Open(new Uri(songFile));
        mediaPlayer.Play();
        mediaPlayer.Volume = VolumeSlider.Value / 100f;
        if (currentCollection != songCollection || QueueSongs.Count != currentCollection.Songs.Count)
        {
            currentCollection = songCollection;
            PopulateSongQueueFromSongAndCollection(song, currentCollection);
        }
        else if (currentCollection == songCollection)
        {
            int songIndex = QueueSongs.IndexOf(new(song, songCollection));
            if (songIndex != -1)
                // Move the current song to the end of the queue
                QueueSongs.Move(songIndex, QueueSongs.Count - 1);
        }
        currentSong = song;
        ProgressSlider.Value = 0;

        LoadLyricsInView();
    }

    /// <summary>
    /// Stops the playback.
    /// </summary>
    private void StopPlayback()
    {
        mediaPlayer.Close();
        mediaPlayer.Stop();
        IsPlaying = false;
    }

    /// <summary>
    /// Resets the playback.
    /// </summary>
    private void ResetPlayback()
    {
        StopPlayback();
        QueueSongs.Clear();
        currentCollection = null;
        IsPlaying = false;
        ProgressLabel.Content = "00:00 / 00:00";
        ProgressSlider.Value = 0d;
        MiniPlayerWindow.Instance?.SetProgress(0d);
        CurrentSongTitleLabel.Content = "No song playing";
        CurrentSongArtistAlbumLabel.Content = "Artist - Album";
        SetSongLyricsRichTextBoxText(string.Empty);
    }

    public void PreviousSongInPlaylist()
    {
        if (currentCollection is null) return;
        if (QueueSongs.Count == 0)
        {
            PopulateSongQueueFromCollection(currentCollection);
        }
        // Move the last song in the queue (which is the current one) to the beginning
        QueueSongs.Move(QueueSongs.Count - 1, 0);
        // Then play the new last song in the queue
        PlaySong(QueueSongs[^1].Song.FilePath, currentCollection);
    }

    public void NextSongInPlaylist()
    {
        if (currentCollection is null) return;
        if (QueueSongs.Count == 0)
        {
            PopulateSongQueueFromCollection(currentCollection);
        }

        // Move the first song in the queue to the end
        QueueSongs.Move(0, QueueSongs.Count - 1);
        // Then play the new last song in the queue
        PlaySong(QueueSongs[^1].Song.FilePath, currentCollection);
    }

    private void PopulateSongQueueFromCollection(SongCollection collection)
    {
        QueueSongs.Clear();
        //QueueSongs.EnsureCapacity(collection.Songs.Count);
        Song[] qSongs = collection.Songs.Values.ToArray();
        random.Shuffle(qSongs);
        for (int i = 0; i < qSongs.Length; i++)
        {
            var songCsei = AllSongs?.Where(csei => csei.Song == qSongs[i]).First();
            if (songCsei is null) continue;
            QueueSongs.Add(songCsei);
        }
    }

    private void PopulateSongQueueFromSongAndCollection(Song song, SongCollection collection)
    {
        QueueSongs.Clear();
        //songQueue.EnsureCapacity(collection.Songs.Count);
        Song[] qSongs = (from s in collection.Songs.Values.ToArray() where !s.Equals(song) select s).ToArray();
        random.Shuffle(qSongs);

        for (int i = 0; i < qSongs.Length; i++)
        {
            var songCsei = AllSongs?.Where(csei => csei.Song == qSongs[i]).FirstOrDefault();
            if (songCsei is null) continue;
            QueueSongs.Add(songCsei);
        }
        // Add the specified song at the end because it's technically currently playing
        var thisSongElementInfo = AllSongs?.Where(csei => csei.Song == song).FirstOrDefault();
        if (thisSongElementInfo is not null)
        {
            QueueSongs.Add(thisSongElementInfo);
        }
    }

    private void MediaPlayer_MediaOpened(object? sender, EventArgs e)
    {
        if (!mediaPlayer.NaturalDuration.HasTimeSpan) return;
        ProgressSlider.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalMilliseconds;
        ProgressSlider.Value = 0;
        MiniPlayerWindow.Instance?.SetMaximumProgress(ProgressSlider.Maximum);
        MiniPlayerWindow.Instance?.SetProgress(0d);
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
            PlaySong(currentSong.FilePath, currentCollection!);
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
        PlayPauseButtonIcon.Content = playing ? Application.Current.FindResource("PauseIcon") : Application.Current.FindResource("PlayIcon");
    }

    private void IncreaseVolume()
    {
        mediaPlayer.Volume += (double)VOLUME_STEP / 100f;
        VolumeSlider.Value += VOLUME_STEP;
        VolumeLabel.Content = $"Volume: {VolumeSlider.Value}%";
        Properties.Settings.Default.PlayerVolume = VolumeSlider.Value;
        Properties.Settings.Default.Save();
    }

    private void DecreaseVolume()
    {
        mediaPlayer.Volume -= (double)VOLUME_STEP / 100f;
        VolumeSlider.Value -= VOLUME_STEP;
        VolumeLabel.Content = $"Volume: {VolumeSlider.Value}%";
        Properties.Settings.Default.PlayerVolume = VolumeSlider.Value;
        Properties.Settings.Default.Save();
    }

    private void ToggleLoop()
    {
        LoopToggleButton.IsChecked = !LoopToggleButton.IsChecked;
        Properties.Settings.Default.PlayerLoop = (bool)LoopToggleButton.IsChecked!;
        Properties.Settings.Default.Save();
    }

    private void ToggleShuffle()
    {
        ShuffleToggleButton.IsChecked = !ShuffleToggleButton.IsChecked;
        Properties.Settings.Default.PlayerShuffle = (bool)ShuffleToggleButton.IsChecked!;
        Properties.Settings.Default.Save();
    }

    private void GoToBeginningOfSong()
    {
        mediaPlayer.Position = TimeSpan.Zero;
        ProgressSlider.Value = 0;
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
            Properties.Settings.Default.Save();
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

    private void OnSettingsGoToBeginningOfSongShortcutButtonClick(object sender, RoutedEventArgs e)
    {
        ChangeShortcut("GoToBeginningShortcutKey", SettingsGoToBeginningOfSongShortcutButton);
    }

    private void OnSettingsSwitchMiniplayerShortcutButtonClick(object sender, RoutedEventArgs e)
    {
        ChangeShortcut("SwitchMiniplayerShortcutKey", SettingsSwitchMiniplayerShortcutButton);
    }

    private void OnSettingsEnableFadeInCheckboxChecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.PlaybackFadeIn = true;
        Properties.Settings.Default.Save();
    }

    private void OnSettingsEnableFadeInCheckboxUnchecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.PlaybackFadeIn = false;
        Properties.Settings.Default.Save();
    }

    private void OnSettingsEnableFadeOutCheckboxChecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.PlaybackFadeOut = true;
        Properties.Settings.Default.Save();
    }

    private void OnSettingsEnableFadeOutCheckboxUnchecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.PlaybackFadeOut = false;
        Properties.Settings.Default.Save();
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
        if (collection.Songs.Count == 0) return;
        currentCollection = collection;
        PopulateSongQueueFromCollection(currentCollection);
        NextSongInPlaylist();
    }

    public void MoveSongToTopOfQueue(CustomSongElement.CustomSongElementInfo? info)
    {
        if (info is null) return;
        int songIndex = QueueSongs.IndexOf(info);
        if (songIndex != -1)
            QueueSongs.Move(songIndex, 0);
    }

    public void BackFromMiniplayer()
    {
        Show();
        WindowState = lastWindowState;
        Activate();
    }

    private void ShowMiniplayer()
    {
        new MiniPlayerWindow()?.Show();
        MiniPlayerWindow.Instance?.SetTitleText(currentSong?.Title);
        MiniPlayerWindow.Instance?.SetArtistText(currentSong?.Artist);
        MiniPlayerWindow.Instance?.SetPlayPauseImage(IsPlaying);
        MiniPlayerWindow.Instance?.SetCover(currentSong?.Cover);
        MiniPlayerWindow.Instance?.SetMaximumProgress(ProgressSlider.Maximum);
        MiniPlayerWindow.Instance?.SetProgress(ProgressSlider.Value);
        Hide();
    }

    private void SwitchMiniplayerView()
    {
        if (MiniPlayerWindow.Instance == null)
        {
            ShowMiniplayer();
        }
        else
        {
            MiniPlayerWindow.Instance.Close();
        }
    }

    private void MiniplayerButtonClick(object sender, RoutedEventArgs e)
    {
        ShowMiniplayer();
    }

    public void UpdateLoop(bool enabled)
    {
        LoopToggleButton.IsChecked = enabled;
        Properties.Settings.Default.PlayerLoop = enabled;
        Properties.Settings.Default.Save();
    }

    public void UpdateShuffle(bool enabled)
    {
        ShuffleToggleButton.IsChecked = enabled;
        Properties.Settings.Default.PlayerShuffle = enabled;
        Properties.Settings.Default.Save();
    }

    private void UpdatePlayingSongElement(Song song, SongCollection? songCollection)
    {
        if (songCollection is null) return;
        if (currentSongElementInfo is not null) currentSongElementInfo.IsPlaying = false;
        if (currentSongElementInfoListView is not null) currentSongElementInfoListView.Items.Refresh();

        if (songCollection == allSongsPlaylist)
        {
            currentSongElementInfoListView = AllSongsListView;
        }
        else if (songCollection.Type == SongCollectionType.Playlist && songCollection.Name == "__HOMP_SEARCH_RESULTS_PLAYLIST__")
        {
            currentSongElementInfoListView = SearchResultsSongsListView;
        }
        else
        {
            currentSongElementInfoListView = songCollection.Type switch
            {
                SongCollectionType.Playlist => PlaylistSongsListView,
                SongCollectionType.Album => AlbumSongsListView,
                SongCollectionType.Artist => ArtistSongsListView,
                _ => null
            };
        }

        currentSongElementInfo = currentSongElementInfoListView?.ItemsSource
            .OfType<CustomSongElement.CustomSongElementInfo>()
            .Where(a => a.Song == song)
            .FirstOrDefault();
        if (currentSongElementInfo is not null) currentSongElementInfo.IsPlaying = true;
        currentSongElementInfoListView?.Items.Refresh();
    }

    private void SongLyricsRichTextBoxVisibilityButtonClick(object sender, RoutedEventArgs e)
    {
        IsSidebarVisible = (true, !IsSidebarVisible.Val);
    }

    private void OnSettingsMiniplayerAutoOpacityCheckboxChecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.MiniplayerAutoOpacity = true;
        Properties.Settings.Default.Save();
    }

    private void OnSettingsMiniplayerAutoOpacityCheckboxUnchecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.MiniplayerAutoOpacity = false;
        Properties.Settings.Default.Save();
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
        Properties.Settings.Default.Save();
    }

    private void SettingsMiniplayerOpacityTimeoutNumberInputBoxFinishedEditing(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.MiniplayerFadingTimeout = (int)SettingsMiniplayerOpacityTimeoutNumberInputBox.NumericValue;
        Properties.Settings.Default.Save();
    }

    private void SettingsMiniplayerAutoAppearOnMinimizeCheckboxChecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.MiniplayerAppearOnMinimize = true;
        Properties.Settings.Default.Save();
    }

    private void SettingsMiniplayerAutoAppearOnMinimizeCheckboxUnchecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.MiniplayerAppearOnMinimize = false;
        Properties.Settings.Default.Save();
    }

    private void SettingsMiniplayerHideControlsCheckboxChecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.MiniplayerHideControls = true;
        Properties.Settings.Default.Save();
    }

    private void SettingsMiniplayerHideControlsCheckboxUnchecked(object sender, RoutedEventArgs e)
    {
        Properties.Settings.Default.MiniplayerHideControls = false;
        Properties.Settings.Default.Save();
    }

    public void QueueDropEvent(object sender, DragEventArgs e)
    {
        // Current problems:
        //      - current queue position is not updated, so wrong song on next/previous

        CustomSongElement.CustomSongElementInfo? data =
            e.Data.GetData(typeof(CustomSongElement.CustomSongElementInfo))
            as CustomSongElement.CustomSongElementInfo;
        CustomSongElement.CustomSongElementInfo? target =
            ((CustomSongElement)sender)?.DataContext as CustomSongElement.CustomSongElementInfo;

        if (data is null || target is null) return;

        int removedIndex = QueueListView.Items.IndexOf(data);
        int targetIndex = QueueListView.Items.IndexOf(target);

        if (removedIndex < targetIndex)
        {
            QueueSongs.Insert(targetIndex + 1, data);
            QueueSongs.RemoveAt(removedIndex);
        }
        else
        {
            int remIndex = removedIndex + 1;
            if (QueueSongs.Count + 1 > remIndex)
            {
                QueueSongs.Insert(targetIndex, data);
                QueueSongs.RemoveAt(remIndex);
            }
        }
    }
}
