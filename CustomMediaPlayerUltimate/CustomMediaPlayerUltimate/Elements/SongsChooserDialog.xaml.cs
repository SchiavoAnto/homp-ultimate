using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using CustomMediaPlayerUltimate.DataStructures;

namespace CustomMediaPlayerUltimate.Elements;

public partial class SongsChooserDialog : Window
{
    public List<Song> Result { get; set; } = new List<Song>();
    public List<Song> AllSongs;
    public ObservableCollection<SongItem> FilteredSongs { get; set; } = new();
    private SongItem? lastSelected = null;

    private const int GWL_STYLE = -16;
    private const int WS_MINIMIZE = 0x20000;

    [DllImport("user32.dll")]
    public static extern IntPtr SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

    [DllImport("user32.dll")]
    public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

    public SongsChooserDialog(SongCollection source, SongCollection destination)
    {
        InitializeComponent();

        Owner = MainWindow.Instance;
        DataContext = this;

        AllSongs = new(source.Songs.Count);
        foreach (Song song in source.Songs.Values)
        {
            bool isAlreadyPresent = destination.Songs.ContainsKey(song.FilePath);
            if (isAlreadyPresent)
                Result.Add(song);

            AllSongs.Add(song);
            FilteredSongs.Add(new(isAlreadyPresent, song));
        }

        SongsListView.ItemsSource = FilteredSongs;
        SelectedSongsLabel.Content = Utils.Pluralize(Result.Count, "song selected", "songs selected");
    }

    protected override void OnSourceInitialized(EventArgs e)
    {
        base.OnSourceInitialized(e);

        nint hWnd = new WindowInteropHelper(this).Handle;
        int currentStyle = GetWindowLong(hWnd, GWL_STYLE);
        SetWindowLong(hWnd, GWL_STYLE, (currentStyle & ~WS_MINIMIZE));
    }

    private void SearchInputTextBoxTextChanged(object sender, TextChangedEventArgs e)
    {
        FilteredSongs.Clear();
        IEnumerable<Song> correlated = AllSongs.Where(s => s.IsCorrelated(SearchInputTextBox.Text));

        foreach (Song song in correlated)
        {
            bool isAlreadyPresent = Result.Contains(song);
            FilteredSongs.Add(new(isAlreadyPresent, song));
        }
        lastSelected = null;
    }

    private void CancelButtonClick(object? sender, RoutedEventArgs e)
    {
        DialogResult = false;
        Close();
    }

    private void ConfirmButtonClick(object? sender, RoutedEventArgs e)
    {
        DialogResult = true;
        Close();
    }

    private void SongItemClick(object sender, RoutedEventArgs e)
    {
        CheckBox? cb = sender as CheckBox;
        if (cb is null) return;
        if (cb.IsChecked == true)
        {
            SongItemChecked(cb);
        }
        else if (cb.IsChecked == false)
        {
            SongItemUnchecked(cb);
        }
    }

    private void SongItemChecked(CheckBox cb)
    {
        SongItem? item = (cb.Tag as SongItem);
        if (item is null) return;

        if (Keyboard.IsKeyDown(Key.LeftShift))
        {
            if (lastSelected is not null)
            {
                int item1 = FilteredSongs.IndexOf(lastSelected);
                int item2 = FilteredSongs.IndexOf(item);
                if (item2 - item1 <= 0) return;

                for (int i = item1 + 1; i <= item2; i++)
                {
                    if (!FilteredSongs[i].IsSelected || i == item2)
                        Result.Add(FilteredSongs[i].Song);
                    FilteredSongs[i].IsSelected = true;
                }
                lastSelected = null;
                SongsListView.Items.Refresh();
            }
        }
        else
        {
            Song? song = AllSongs.Find(s => s.FilePath == item.Song.FilePath);
            if (song is null) return;
            Result.Add(song);
            lastSelected = item;
        }
        SelectedSongsLabel.Content = Utils.Pluralize(Result.Count, "song selected", "songs selected");
    }

    private void SongItemUnchecked(CheckBox cb)
    {
        SongItem? item = (cb.Tag as SongItem);
        if (item is null) return;

        if (Keyboard.IsKeyDown(Key.LeftShift))
        {
            if (lastSelected is not null)
            {
                int item1 = FilteredSongs.IndexOf(lastSelected);
                int item2 = FilteredSongs.IndexOf(item);
                if (item2 - item1 <= 0) return;

                for (int i = item1 + 1; i <= item2; i++)
                {
                    if (FilteredSongs[i].IsSelected || i == item2)
                        Result.Remove(FilteredSongs[i].Song);
                    FilteredSongs[i].IsSelected = false;
                }
                lastSelected = null;
                SongsListView.Items.Refresh();
            }
        }
        else
        {
            Song? song = Result.Find(s => s.FilePath == item.Song.FilePath);
            if (song is null) return;
            Result.Remove(song);
            lastSelected = item;
        }
        SelectedSongsLabel.Content = Utils.Pluralize(Result.Count, "song selected", "songs selected");
    }
}

public class SongItem
{
    public bool IsSelected { get; set; }
    public Song Song { get; private init; }

    public SongItem(bool selected, Song song)
    {
        IsSelected = selected;
        Song = song;
    }
}